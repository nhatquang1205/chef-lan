using ChefLan.Logic.Courses.Contracts;

namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>Tells staff about an order that needs a human. Best effort: callers never let a failure change their result.</summary>
public interface IOrderNotifier
{
    Task ContactOrderCreatedAsync(ContactOrderNotice notice, CancellationToken cancellationToken = default);
}
