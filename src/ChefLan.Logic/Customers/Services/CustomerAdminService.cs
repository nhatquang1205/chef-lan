using ChefLan.Commons.Auth;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Errors;
using ChefLan.Logic.Customers.Interfaces;
using ChefLan.Logic.Customers.Mapping;
using Microsoft.Extensions.Logging;

namespace ChefLan.Logic.Customers.Services;

internal sealed class CustomerAdminService(
    ICustomerRepository customers,
    ICustomerRefreshTokenRepository refreshTokens,
    ICustomerAccessCounter accessCounter,
    ICustomerEmails emails,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CustomerAdminService> logger) : ICustomerAdminService
{
    /// <summary>How long an invitation link works. A new invite replaces the previous one.</summary>
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<PagedResult<CustomerAdminListItem>> ListAsync(
        CustomerAdminQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await customers.ListAsync(
            new PagedRequest { Page = query.Page, PageSize = query.PageSize },
            Clean(query.Q),
            query.Status,
            clock.UtcNow,
            cancellationToken);

        var counts = await accessCounter.CountActiveAsync(
            page.Items.Select(c => c.Id).ToList(), clock.UtcNow, cancellationToken);

        var items = page.Items
            .Select(c => c.ToListItem(counts.GetValueOrDefault(c.Id), clock.UtcNow))
            .ToList();

        return PagedResult<CustomerAdminListItem>.Create(items, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<Result<CustomerAdminDetail>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        return customer is null ? CustomerErrors.NotFound(id) : customer.ToAdminDetail(clock.UtcNow);
    }

    public async Task<Result<CustomerAdminDetail>> CreateAsync(
        CustomerAdminRequest request,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (email is not null && await customers.EmailExistsAsync(email, cancellationToken))
        {
            return CustomerErrors.EmailAlreadyUsed(email);
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = Clean(request.Phone),
            FacebookName = Clean(request.FacebookName),
            FacebookProfileUrl = Clean(request.FacebookProfileUrl),
            Note = Clean(request.Note),
            CreatedByStaffId = staffId,
        };

        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The customer is kept even when the mail fails: the admin sees the error and can resend.
        if (request.SendInvite && customer.Email is not null)
        {
            var invite = await SendInviteAsync(customer, cancellationToken);

            if (invite.IsFailure)
            {
                return customer.ToAdminDetail(clock.UtcNow) with { InviteError = invite.Error.Code };
            }
        }

        return customer.ToAdminDetail(clock.UtcNow);
    }

    public async Task<Result<CustomerAdminDetail>> UpdateAsync(
        long id,
        CustomerAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        var email = NormalizeEmail(request.Email);

        if (!string.Equals(email, customer.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (email is not null)
            {
                var owner = await customers.GetByEmailAsync(email, cancellationToken);

                if (owner is not null && owner.Id != customer.Id)
                {
                    return CustomerErrors.EmailAlreadyUsed(email);
                }
            }

            customer.Email = email;

            // The pending link went to the old mailbox; it must not activate the new address.
            customer.InviteTokenHash = null;
            customer.InviteExpiresAt = null;
        }

        customer.FullName = request.FullName.Trim();
        customer.Phone = Clean(request.Phone);
        customer.FacebookName = Clean(request.FacebookName);
        customer.FacebookProfileUrl = Clean(request.FacebookProfileUrl);
        customer.Note = Clean(request.Note);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToAdminDetail(clock.UtcNow);
    }

    public async Task<Result<CustomerInviteResponse>> InviteAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        if (customer.Email is null)
        {
            return CustomerErrors.NoEmail;
        }

        if (customer.PasswordHash is not null)
        {
            return CustomerErrors.AlreadyActive;
        }

        if (!customer.IsActive)
        {
            return CustomerErrors.AccountInactive;
        }

        return await SendInviteAsync(customer, cancellationToken);
    }

    /// <summary>
    /// Mails first, saves after: if the mail fails, the previous invitation (if any) still works and nothing changed.
    /// </summary>
    private async Task<Result<CustomerInviteResponse>> SendInviteAsync(Customer customer, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var expiresAt = now + InvitationLifetime;
        var (token, hash) = OneTimeToken.Create();

        try
        {
            await emailSender.SendAsync(emails.Invitation(customer.Email!, customer.FullName, token, expiresAt), cancellationToken);
        }
        catch (EmailSendException ex)
        {
            logger.LogWarning(ex, "Invitation mail to customer {CustomerId} failed.", customer.Id);
            return CustomerErrors.EmailSendFailed;
        }

        customer.InviteTokenHash = hash;
        customer.InviteExpiresAt = expiresAt;
        customer.InvitedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerInviteResponse(now, expiresAt);
    }

    public async Task<Result> DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound(id));
        }

        customer.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await refreshTokens.RevokeAllForCustomerAsync(customer.Id, clock.UtcNow, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound(id));
        }

        customer.IsActive = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
