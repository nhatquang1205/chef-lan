using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Mapping;

internal static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Email, customer.FullName, customer.Phone);

    public static CustomerStatus StatusOf(this Customer c)
    {
        if (!c.IsActive)
        {
            return CustomerStatus.Inactive;
        }

        if (c.PasswordHash is not null || c.ActivatedAt is not null)
        {
            return CustomerStatus.Active;
        }

        // Invite expiry columns arrive with invitations; until then an invited customer stays Invited.
        return c.InvitedAt is not null ? CustomerStatus.Invited : CustomerStatus.NotInvited;
    }

    public static CustomerAdminListItem ToListItem(this Customer c, CustomerAccessCounts counts) =>
        new(c.Id, c.FullName, c.Email, c.Phone, c.FacebookName, c.StatusOf(),
            c.InvitedAt, c.ActivatedAt, c.LastLoginAt, c.CreatedAt, counts.Courses, counts.Lessons);

    public static CustomerAdminDetail ToAdminDetail(this Customer c) =>
        new(c.Id, c.FullName, c.Email, c.Phone, c.FacebookName, c.FacebookProfileUrl, c.Note, c.IsActive,
            c.StatusOf(), c.InvitedAt, null, c.ActivatedAt, c.LastLoginAt, c.CreatedAt);
}
