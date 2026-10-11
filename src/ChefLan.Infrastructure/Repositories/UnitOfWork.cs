using ChefLan.DB.Contexts;
using ChefLan.Logic.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ChefLan.Infrastructure.Repositories;

internal sealed class UnitOfWork(CoursesDbContext context) : IUnitOfWork
{
    private const string UniqueViolation = "23505";

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The record was changed by someone else. Reload and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            throw new ConflictException("A record with the same unique value already exists.", ex);
        }
    }
}
