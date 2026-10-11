using ChefLan.Commons.Auth;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Identity.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Identity.Contracts;
using ChefLan.Logic.Identity.Errors;
using ChefLan.Logic.Identity.Interfaces;
using ChefLan.Logic.Identity.Mapping;

namespace ChefLan.Logic.Identity.Services;

internal sealed class StaffAuthService(
    IUserRepository users,
    IUserRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt,
    IClock clock) : IStaffAuthService
{
    public async Task<Result<StaffAuthResponse>> LoginAsync(
        StaffLoginRequest request,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var user = await users.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        if (user is null)
        {
            passwordHasher.Verify(request.Password, LoginSecurity.DummyPasswordHash);
            return IdentityErrors.InvalidCredentials;
        }

        if (user.LockedUntil > now)
        {
            return IdentityErrors.AccountLocked;
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= LoginSecurity.MaxFailedAttempts)
            {
                user.LockedUntil = now + LoginSecurity.LockDuration;
                user.FailedLoginCount = 0;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return IdentityErrors.InvalidCredentials;
        }

        if (!user.IsActive)
        {
            return IdentityErrors.AccountInactive;
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = now;

        var response = IssuePair(user, client, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<Result<StaffAuthResponse>> RefreshAsync(
        StaffRefreshRequest request,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var token = await refreshTokens.GetByHashAsync(
            RefreshTokenGenerator.Hash(request.RefreshToken), cancellationToken);

        if (token is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        if (token.RevokedAt is not null)
        {
            // Revoked AND rotated means the chain was replayed: someone holds a stolen copy.
            if (token.ReplacedByTokenHash is not null)
            {
                await refreshTokens.RevokeAllForUserAsync(token.UserId, now, cancellationToken);
            }

            return IdentityErrors.InvalidRefreshToken;
        }

        if (token.ExpiresAt <= now)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var user = token.User;

        if (user is null || user.IsDeleted || !user.IsActive)
        {
            token.RevokedAt = now;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return IdentityErrors.InvalidRefreshToken;
        }

        var response = IssuePair(user, client, now, out var newHash);
        token.RevokedAt = now;
        token.ReplacedByTokenHash = newHash;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<Result> LogoutAsync(
        long userId,
        StaffLogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await refreshTokens.GetByHashAsync(
            RefreshTokenGenerator.Hash(request.RefreshToken), cancellationToken);

        if (token is { RevokedAt: null } && token.UserId == userId)
        {
            token.RevokedAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        long userId,
        StaffChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var user = await users.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure(IdentityErrors.AccountInactive);
        }

        if (user.LockedUntil > now)
        {
            return Result.Failure(IdentityErrors.AccountLocked);
        }

        // Counts toward lockout, so a stolen access token cannot be used to guess the password.
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= LoginSecurity.MaxFailedAttempts)
            {
                user.LockedUntil = now + LoginSecurity.LockDuration;
                user.FailedLoginCount = 0;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(IdentityErrors.CurrentPasswordIncorrect);
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.FailedLoginCount = 0;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await refreshTokens.RevokeAllForUserAsync(userId, now, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<StaffResponse>> GetMeAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return IdentityErrors.UserNotFound(userId);
        }

        return user.IsActive ? user.ToResponse() : IdentityErrors.AccountInactive;
    }

    private StaffAuthResponse IssuePair(User user, ClientInfo client, DateTime now) =>
        IssuePair(user, client, now, out _);

    private StaffAuthResponse IssuePair(User user, ClientInfo client, DateTime now, out string tokenHash)
    {
        var access = jwt.CreateStaffToken(new StaffTokenData(
            user.Id, user.Email, user.DisplayName, user.Role.ToString()));
        var (raw, hash) = RefreshTokenGenerator.Create();

        refreshTokens.Add(new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = now + jwt.RefreshTokenLifetime(ChefLanAuthSchemes.Internal),
            CreatedIp = client.SafeIp,
            UserAgent = client.SafeUserAgent,
        });

        tokenHash = hash;
        return new StaffAuthResponse(access.Value, access.ExpiresAt, raw, user.ToResponse());
    }
}
