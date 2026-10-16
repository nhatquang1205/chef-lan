using ChefLan.DB.Contexts;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Customers;

/// <summary>Same context owns customers and courses, so this is two grouped counts; "active" matches IsActiveAt.</summary>
internal sealed class CustomerAccessCounter(CoursesDbContext context) : ICustomerAccessCounter
{
    public async Task<IReadOnlyDictionary<long, CustomerAccessCounts>> CountActiveAsync(
        IReadOnlyCollection<long> customerIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
        {
            return new Dictionary<long, CustomerAccessCounts>();
        }

        var courses = await context.Enrollments
            .Where(e => customerIds.Contains(e.CustomerId)
                        && e.Status == EnrollmentStatus.Active
                        && (e.ExpiresAt == null || e.ExpiresAt > utcNow))
            .GroupBy(e => e.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, cancellationToken);

        var lessons = await context.LessonEnrollments
            .Where(e => customerIds.Contains(e.CustomerId)
                        && e.Status == EnrollmentStatus.Active
                        && (e.ExpiresAt == null || e.ExpiresAt > utcNow))
            .GroupBy(e => e.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Count, cancellationToken);

        return customerIds.ToDictionary(
            id => id,
            id => new CustomerAccessCounts(courses.GetValueOrDefault(id), lessons.GetValueOrDefault(id)));
    }
}
