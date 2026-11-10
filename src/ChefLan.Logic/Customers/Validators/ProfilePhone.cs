using System.Text.RegularExpressions;

namespace ChefLan.Logic.Customers.Validators;

internal static partial class ProfilePhone
{
    /// <summary>Strips spaces and dots, then accepts an optional leading + and 9 to 15 digits.</summary>
    public static bool TryNormalize(string? input, out string phone)
    {
        phone = (input ?? string.Empty).Replace(" ", "").Replace(".", "");

        return PhonePattern().IsMatch(phone);
    }

    [GeneratedRegex(@"^\+?[0-9]{9,15}$")]
    private static partial Regex PhonePattern();
}
