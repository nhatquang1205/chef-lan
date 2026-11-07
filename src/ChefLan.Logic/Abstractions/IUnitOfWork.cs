using ChefLan.Commons.Results;

namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Commits the work a use case has queued up. Implemented in ChefLan.DB over the
/// DbContext, so the application layer never sees SaveChanges.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> (which may save several times) in one transaction: committed on success,
    /// rolled back on a failed result or an exception.
    /// </summary>
    Task<Result<T>> InTransactionAsync<T>(Func<Task<Result<T>>> work, CancellationToken cancellationToken = default);
}
