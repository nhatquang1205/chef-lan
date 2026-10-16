using ChefLan.Commons.Auth;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Errors;
using ChefLan.Logic.Customers.Interfaces;
using ChefLan.Logic.Customers.Mapping;

namespace ChefLan.Logic.Customers.Services;

internal sealed class CustomerAuthService(
    ICustomerRepository customers,
    ICustomerRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt,
    IClock clock) : ICustomerAuthService
{
    public async Task<Result<CustomerAuthResponse>> RegisterAsync(
        CustomerRegisterRequest request,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        if (await customers.EmailExistsAsync(email, cancellationToken))
        {
            return CustomerErrors.EmailAlreadyUsed(email);
        }

        var now = clock.UtcNow;
        var customer = new Customer
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            ActivatedAt = now,
            LastLoginAt = now,
        };

        customers.Add(customer);

        // The access token's "sub" is the generated id, so the row must exist first.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = IssuePair(customer, client, now, out _);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<Result<CustomerAuthResponse>> LoginAsync(
        CustomerLoginRequest request,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var customer = await customers.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        if (customer is null)
        {
            passwordHasher.Verify(request.Password, LoginSecurity.DummyPasswordHash);
            return CustomerErrors.InvalidCredentials;
        }

        if (customer.LockedUntil > now)
        {
            return CustomerErrors.AccountLocked;
        }

        // Recorded by staff (Facebook-era buyer) and never invited: nothing to verify.
        if (customer.PasswordHash is null)
        {
            return CustomerErrors.NotActivated;
        }

        if (!passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            customer.FailedLoginCount++;

            if (customer.FailedLoginCount >= LoginSecurity.MaxFailedAttempts)
            {
                customer.LockedUntil = now + LoginSecurity.LockDuration;
                customer.FailedLoginCount = 0;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return CustomerErrors.InvalidCredentials;
        }

        if (!customer.IsActive)
        {
            return CustomerErrors.AccountInactive;
        }

        customer.FailedLoginCount = 0;
        customer.LockedUntil = null;
        customer.LastLoginAt = now;

        var response = IssuePair(customer, client, now, out _);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<Result<CustomerAuthResponse>> RefreshAsync(
        CustomerRefreshRequest request,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var token = await refreshTokens.GetByHashAsync(
            RefreshTokenGenerator.Hash(request.RefreshToken), cancellationToken);

        if (token is null)
        {
            return CustomerErrors.InvalidRefreshToken;
        }

        if (token.RevokedAt is not null)
        {
            if (token.ReplacedByTokenHash is not null)
            {
                await refreshTokens.RevokeAllForCustomerAsync(token.CustomerId, now, cancellationToken);
            }

            return CustomerErrors.InvalidRefreshToken;
        }

        if (token.ExpiresAt <= now)
        {
            return CustomerErrors.InvalidRefreshToken;
        }

        var customer = token.Customer;

        if (customer is null || customer.IsDeleted || !customer.IsActive || customer.Email is null)
        {
            token.RevokedAt = now;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return CustomerErrors.InvalidRefreshToken;
        }

        var response = IssuePair(customer, client, now, out var newHash);
        token.RevokedAt = now;
        token.ReplacedByTokenHash = newHash;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<Result> LogoutAsync(
        long customerId,
        CustomerLogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await refreshTokens.GetByHashAsync(
            RefreshTokenGenerator.Hash(request.RefreshToken), cancellationToken);

        if (token is { RevokedAt: null } && token.CustomerId == customerId)
        {
            token.RevokedAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<InvitationInfoResponse>> InspectInvitationAsync(
        InvitationTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await FindInvitedAsync(request.Token, cancellationToken);

        return customer is null
            ? CustomerErrors.InviteInvalid
            : new InvitationInfoResponse(customer.Email!, customer.FullName);
    }

    /// <summary>
    /// Sets the first password and signs the customer in. Opening the mailed link proves the mailbox, so the
    /// email counts as confirmed. The token is cleared, so the link works once.
    /// </summary>
    public async Task<Result<CustomerAuthResponse>> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var customer = await FindInvitedAsync(request.Token, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.InviteInvalid;
        }

        var now = clock.UtcNow;
        customer.PasswordHash = passwordHasher.Hash(request.Password);
        customer.ActivatedAt = now;
        customer.EmailConfirmedAt ??= now;
        customer.InviteTokenHash = null;
        customer.InviteExpiresAt = null;
        customer.FailedLoginCount = 0;
        customer.LockedUntil = null;
        customer.LastLoginAt = now;

        var response = IssuePair(customer, client, now, out _);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    /// <summary>
    /// The customer a still-valid invitation belongs to, or null. Unknown, used and expired tokens all look the same
    /// to the caller. Someone who already has a password (activated another way) cannot be re-activated by an old link.
    /// </summary>
    private async Task<Customer?> FindInvitedAsync(string token, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByInviteTokenHashAsync(OneTimeToken.Hash(token), cancellationToken);

        return customer is { IsActive: true, IsDeleted: false, PasswordHash: null, Email: not null }
            && customer.InviteExpiresAt > clock.UtcNow
            ? customer
            : null;
    }

    public async Task<Result<CustomerResponse>> GetMeAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(customerId, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(customerId);
        }

        return customer.IsActive ? customer.ToResponse() : CustomerErrors.AccountInactive;
    }

    private CustomerAuthResponse IssuePair(Customer customer, ClientInfo client, DateTime now, out string tokenHash)
    {
        var access = jwt.CreateCustomerToken(new CustomerTokenData(
            customer.Id, customer.Email!, customer.FullName));
        var (raw, hash) = RefreshTokenGenerator.Create();

        refreshTokens.Add(new CustomerRefreshToken
        {
            CustomerId = customer.Id,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = now + jwt.RefreshTokenLifetime(ChefLanAuthSchemes.Customer),
            CreatedIp = client.SafeIp,
            UserAgent = client.SafeUserAgent,
        });

        tokenHash = hash;
        return new CustomerAuthResponse(access.Value, access.ExpiresAt, raw, customer.ToResponse());
    }
}
