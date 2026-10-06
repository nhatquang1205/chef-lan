namespace ChefLan.Domain.Common;

/// <summary>Maps to the Postgres system column xmin. Costs no storage.</summary>
public interface IHasRowVersion
{
    uint RowVersion { get; set; }
}
