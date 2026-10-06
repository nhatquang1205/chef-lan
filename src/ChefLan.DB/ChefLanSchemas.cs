namespace ChefLan.DB;

/// <summary>
/// One schema per concern. A DbContext may only map tables in schemas it owns, and a
/// foreign key may not cross a schema boundary unless one context owns both sides.
///
/// Notably: nothing in <see cref="Identity"/> ever references <see cref="Customers"/>, or
/// the reverse. Staff and customers are disjoint populations.
/// </summary>
public static class ChefLanSchemas
{
    /// <summary>Internal staff accounts.</summary>
    public const string Identity = "identity";

    /// <summary>External customer accounts and profiles.</summary>
    public const string Customers = "customers";

    /// <summary>Courses, lessons, categories, enrollments.</summary>
    public const string Courses = "courses";

    public const string MigrationsHistoryTable = "__ef_migrations_history";
}
