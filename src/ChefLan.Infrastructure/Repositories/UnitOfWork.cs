using ChefLan.Commons.Results;
using ChefLan.DB.Contexts;
using ChefLan.Logic.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ChefLan.Infrastructure.Repositories;

internal sealed class UnitOfWork(CoursesDbContext context) : IUnitOfWork
{
    private const string UniqueViolation = "23505";

    public async Task<Result<T>> InTransactionAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await work();
        }

        // The context retries transient failures, which only works when the whole transaction is the retried unit.
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var result = await work();

            if (result.IsSuccess)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return result;
        });
    }

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
