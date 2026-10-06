namespace ChefLan.Domain.Common;

public abstract class AuditableSoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByUserId { get; set; }
}
