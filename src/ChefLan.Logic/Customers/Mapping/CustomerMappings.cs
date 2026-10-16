using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Mapping;

internal static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Email, customer.FullName, customer.Phone);

    public static CustomerStatus StatusOf(this Customer c, DateTime utcNow)
    {
        if (!c.IsActive)
        {
            return CustomerStatus.Inactive;
        }

        if (c.PasswordHash is not null || c.ActivatedAt is not null)
        {
            return CustomerStatus.Active;
        }

        if (c.InviteTokenHash is null)
        {
            return CustomerStatus.NotInvited;
        }

        return c.InviteExpiresAt > utcNow ? CustomerStatus.Invited : CustomerStatus.InviteExpired;
    }

    public static CustomerAdminListItem ToListItem(this Customer c, CustomerAccessCounts counts, DateTime utcNow) =>
        new(c.Id, c.FullName, c.Email, c.Phone, c.FacebookName, c.StatusOf(utcNow),
            c.InvitedAt, c.ActivatedAt, c.LastLoginAt, c.CreatedAt, counts.Courses, counts.Lessons);

    public static CustomerAdminDetail ToAdminDetail(this Customer c, DateTime utcNow) =>
        new(c.Id, c.FullName, c.Email, c.Phone, c.FacebookName, c.FacebookProfileUrl, c.Note, c.IsActive,
            c.StatusOf(utcNow), c.InvitedAt, c.InviteExpiresAt, c.ActivatedAt, c.LastLoginAt, c.CreatedAt);
}
