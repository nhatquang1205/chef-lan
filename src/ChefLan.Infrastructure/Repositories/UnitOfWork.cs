using ChefLan.DB.Contexts;
using ChefLan.Logic.Abstractions;

namespace ChefLan.Infrastructure.Repositories;

internal sealed class UnitOfWork(CoursesDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
