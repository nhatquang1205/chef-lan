namespace ChefLan.Commons.Time;

/// <summary>
/// The only sanctioned source of time. DateTime.Now is banned: Npgsql throws when writing
/// a non-UTC DateTime to timestamptz, and every timestamp in this system is UTC.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
