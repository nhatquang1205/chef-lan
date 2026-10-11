using ChefLan.Commons.Text;

namespace ChefLan.Logic.Courses.Services;

internal static class SlugAllocator
{
    /// <summary>
    /// A unique slug for <paramref name="input"/>: -2, -3 on collision. Falls back to
    /// <paramref name="fallback"/> when the input has no sluggable characters at all ("!!!").
    /// The unique index is still the final arbiter; a lost race surfaces as a 409.
    /// </summary>
    public static async Task<string> AllocateAsync(
        ISlugGenerator generator,
        string input,
        string fallback,
        int maxLength,
        Func<string, CancellationToken, Task<IReadOnlySet<string>>> loadTaken,
        CancellationToken cancellationToken)
    {
        var seed = generator.Generate(input, maxLength).Length == 0 ? fallback : input;
        var taken = await loadTaken(generator.Generate(seed, maxLength), cancellationToken);

        return generator.GenerateUnique(seed, taken, maxLength);
    }

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
