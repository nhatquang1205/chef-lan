using System.Globalization;
using System.Text;

namespace ChefLan.Commons.Text;

/// <summary>
/// "Khóa học bánh mì truyền thống" -> "khoa-hoc-banh-mi-truyen-thong".
///
/// Uses an EXPLICIT Vietnamese character map rather than relying on Unicode NFD
/// decomposition. NFD is not dependable here: under invariant globalization (no ICU —
/// which is the default in some trimmed and Alpine-based container images)
/// string.Normalize(FormD) silently returns the composed string instead of throwing, so
/// every accented letter is dropped and "Món Bắc" becomes "m-n-b-c". Slugs are SEO URLs,
/// so a silent, deployment-dependent corruption is the worst possible failure mode.
///
/// NFD still runs afterwards as a best-effort pass for non-Vietnamese input, but nothing
/// Vietnamese depends on it.
/// </summary>
public sealed class VietnameseSlugGenerator : ISlugGenerator
{
    /// <summary>Every Vietnamese letter that carries a diacritic, grouped by its base letter.</summary>
    private static readonly (string Accented, char Base)[] VietnameseGroups =
    [
        ("àáảãạâầấẩẫậăằắẳẵặ", 'a'),
        ("ÀÁẢÃẠÂẦẤẨẪẬĂẰẮẲẴẶ", 'A'),
        ("èéẻẽẹêềếểễệ", 'e'),
        ("ÈÉẺẼẸÊỀẾỂỄỆ", 'E'),
        ("ìíỉĩị", 'i'),
        ("ÌÍỈĨỊ", 'I'),
        ("òóỏõọôồốổỗộơờớởỡợ", 'o'),
        ("ÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢ", 'O'),
        ("ùúủũụưừứửữự", 'u'),
        ("ÙÚỦŨỤƯỪỨỬỮỰ", 'U'),
        ("ỳýỷỹỵ", 'y'),
        ("ỲÝỶỸỴ", 'Y'),

        // đ/Đ are distinct letters, not composed ones, so even a working NFD never
        // decomposes them.
        ("đ", 'd'),
        ("Đ", 'D'),

        // Common non-Vietnamese through-stroke letters, for imported titles.
        ("øðł", 'o'),
        ("ØÐŁ", 'O'),
    ];

    private static readonly Dictionary<char, char> AsciiFolding = BuildFoldingMap();

    public string Generate(string input, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var folded = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            folded.Append(AsciiFolding.TryGetValue(c, out var replacement) ? replacement : c);
        }

        // Best effort for anything outside the Vietnamese map. A no-op without ICU, which
        // is fine — the map above has already handled every Vietnamese letter.
        var decomposed = SafeDecompose(folded.ToString());

        var slug = new StringBuilder(decomposed.Length);
        var lastWasHyphen = false;

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && slug.Length > 0)
            {
                slug.Append('-');
                lastWasHyphen = true;
            }
        }

        var result = slug.ToString().Trim('-');

        return result.Length > maxLength ? result[..maxLength].TrimEnd('-') : result;
    }

    public string GenerateUnique(string input, IReadOnlySet<string> taken, int maxLength = 160)
    {
        var baseSlug = Generate(input, maxLength);

        if (baseSlug.Length == 0 || !taken.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (var suffix = 2; ; suffix++)
        {
            var suffixText = $"-{suffix}";
            var trimmed = baseSlug.Length + suffixText.Length > maxLength
                ? baseSlug[..(maxLength - suffixText.Length)].TrimEnd('-')
                : baseSlug;

            var candidate = trimmed + suffixText;

            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private static Dictionary<char, char> BuildFoldingMap()
    {
        var map = new Dictionary<char, char>();

        foreach (var (accented, baseChar) in VietnameseGroups)
        {
            foreach (var c in accented)
            {
                map[c] = baseChar;
            }
        }

        return map;
    }

    private static string SafeDecompose(string value)
    {
        try
        {
            return value.Normalize(NormalizationForm.FormD);
        }
        catch (ArgumentException)
        {
            // Invalid surrogate pairs etc. The folded string is already good enough.
            return value;
        }
    }
}
