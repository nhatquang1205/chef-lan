using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ChefLan.Logic.Courses.Payments;

/// <summary>
/// The order code that goes in a bank transfer note. Banks mangle notes (case, spaces, accents, added
/// prefixes), so the alphabet leaves out look-alikes (0/O, 1/I) and a code is found by squashing the
/// note to A-Z and 0-9 and searching it, never by exact comparison.
/// </summary>
public static class OrderCodes
{
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public const int RandomLength = 6;

    public static string Generate(string prefix)
    {
        var chars = new char[RandomLength];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return Normalize(prefix) + new string(chars);
    }

    /// <summary>Uppercase, with everything but A-Z and 0-9 removed. Accents are not transliterated: they simply drop out.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            var upper = char.ToUpperInvariant(ch);

            if (upper is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            {
                builder.Append(upper);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Every string in the note (or the provider's own code field) that looks like an order code, best guess
    /// first: the provider's code field when it fits, then each match in the squashed note. The caller tries
    /// them until one names an order, so a payer whose name happens to contain the prefix does no harm.
    /// </summary>
    public static IReadOnlyList<string> FindCandidates(string? providerCode, string? content, string prefix)
    {
        var normalizedPrefix = Normalize(prefix);
        var pattern = new Regex($"{Regex.Escape(normalizedPrefix)}[{Alphabet}]{{{RandomLength}}}", RegexOptions.CultureInvariant);
        var found = new List<string>();

        var fromProvider = Normalize(providerCode);

        if (pattern.IsMatch(fromProvider) && fromProvider.Length == normalizedPrefix.Length + RandomLength)
        {
            found.Add(fromProvider);
        }

        // Matches may overlap (a name ending in the prefix), so step one character at a time.
        var squashed = Normalize(content);

        for (var start = 0; start < squashed.Length; start++)
        {
            var match = pattern.Match(squashed, start);

            if (!match.Success)
            {
                break;
            }

            if (!found.Contains(match.Value))
            {
                found.Add(match.Value);
            }

            start = match.Index;
        }

        return found;
    }
}
