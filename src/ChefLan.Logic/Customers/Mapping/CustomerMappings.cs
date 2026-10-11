using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Mapping;

internal static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Email, customer.FullName, customer.Phone);
}
