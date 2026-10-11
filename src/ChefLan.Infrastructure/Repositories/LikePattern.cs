namespace ChefLan.Infrastructure.Repositories;

internal static class LikePattern
{
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// Escapes LIKE metacharacters so a search for "%" or "_" matches those literal
    /// characters instead of acting as a wildcard. Not an injection risk — EF parameterises
    /// the pattern — but without it, searching "%" returns everything. The backslash must be
    /// escaped first, or it would double-escape the others.
    /// </summary>
    public static string Contains(string input) =>
        "%" + input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
