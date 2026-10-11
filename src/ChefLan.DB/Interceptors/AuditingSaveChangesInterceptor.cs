using ChefLan.Commons.Auth;
using ChefLan.Commons.Time;
using ChefLan.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChefLan.DB.Interceptors;

/// <summary>
/// Fills CreatedAt / UpdatedAt / CreatedByUserId / UpdatedByUserId. Timestamps always come
/// from <see cref="IClock"/> so they are UTC — Npgsql rejects anything else on timestamptz.
///
/// The actor is the STAFF user when one is signed in. Customer-initiated writes leave the
/// actor null, because a customer id in a column named *UserId would be a lie.
/// </summary>
public sealed class AuditingSaveChangesInterceptor(IClock clock, ICurrentUser currentUser)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        long? actorId = currentUser.TryGetStaffId(out var staffId) ? staffId : null;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedByUserId = actorId;
                    entry.Entity.UpdatedByUserId = actorId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = actorId;
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedByUserId).IsModified = false;
                    break;
            }
        }
    }
}
