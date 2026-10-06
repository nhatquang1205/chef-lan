namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Commits the work a use case has queued up. Implemented in ChefLan.DB over the
/// DbContext, so the application layer never sees SaveChanges.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
