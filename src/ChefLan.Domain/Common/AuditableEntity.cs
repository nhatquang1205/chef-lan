namespace ChefLan.Domain.Common;

public abstract class AuditableEntity : IAuditable
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long? CreatedByUserId { get; set; }

    public long? UpdatedByUserId { get; set; }
}
