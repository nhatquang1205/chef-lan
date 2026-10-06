namespace ChefLan.Domain.Common;

/// <summary>Set by the auditing interceptor in ChefLan.DB. Never assign these by hand.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }

    long? CreatedByUserId { get; set; }

    long? UpdatedByUserId { get; set; }
}
