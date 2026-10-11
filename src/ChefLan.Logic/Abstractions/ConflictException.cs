namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Thrown by <see cref="IUnitOfWork"/> when a save loses a race — a stale row version or a
/// unique index hit by a concurrent insert. The API answers 409. Expected business
/// conflicts travel as a Result; this exists only for races the database arbitrates.
/// </summary>
public sealed class ConflictException(string message, Exception? inner = null) : Exception(message, inner);
