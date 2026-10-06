namespace ChefLan.Commons.Text;

public interface ISlugGenerator
{
    /// <summary>
    /// Builds a URL-safe slug. Always generate — never trust a client-supplied slug.
    /// </summary>
    string Generate(string input, int maxLength = 160);

    /// <summary>
    /// Appends -2, -3, ... until the slug is not in <paramref name="taken"/>.
    /// </summary>
    string GenerateUnique(string input, IReadOnlySet<string> taken, int maxLength = 160);
}
