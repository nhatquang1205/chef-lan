using System.Globalization;

namespace ChefLan.Commons.Money;

/// <summary>
/// Vietnamese dong has no minor unit, so amounts are whole numbers — numeric(12,0) in the
/// database. A price with a fractional part is always a bug.
/// </summary>
public static class Vnd
{
    public const string CurrencyCode = "VND";

    private static readonly NumberFormatInfo Format = new() { NumberGroupSeparator = ".", NumberDecimalDigits = 0 };

    public static bool IsValid(decimal amount) => amount >= 0 && decimal.Truncate(amount) == amount;

    public static decimal Normalize(decimal amount) => decimal.Truncate(amount);

    /// <summary>Formats 1200000 as "1.200.000 ₫".</summary>
    public static string ToDisplay(decimal amount) => $"{amount.ToString("N0", Format)} ₫";
}
