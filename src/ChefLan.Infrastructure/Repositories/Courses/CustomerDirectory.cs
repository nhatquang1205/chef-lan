using ChefLan.DB.Contexts;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

/// <summary>
/// The Courses domain's port onto customers. Same database and context, so a plain query; the soft-delete
/// filter means a deleted customer is simply not found. The email column is citext: case-insensitive.
/// </summary>
internal sealed class CustomerDirectory(CoursesDbContext context) : ICustomerDirectory
{
    public Task<CustomerRef?> FindAsync(long? customerId, string? email, CancellationToken cancellationToken = default) =>
        context.Customers
            .AsNoTracking()
            .Where(c => customerId != null ? c.Id == customerId : c.Email != null && c.Email == email)
            .Select(c => new CustomerRef(c.Id, c.Email, c.FullName, c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
}
