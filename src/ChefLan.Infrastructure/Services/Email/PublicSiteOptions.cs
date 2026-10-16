namespace ChefLan.Infrastructure.Services.Email;

/// <summary>The customer website, for links inside mails. Bound from the <c>Web</c> section.</summary>
public sealed class PublicSiteOptions
{
    public const string SectionName = "Web";

    /// <summary>Origin of the customer website, e.g. https://bonbocon.vn — no trailing slash.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
