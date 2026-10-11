namespace ChefLan.Infrastructure.Services.Storage;

/// <summary>
/// Bound from the Storage:Bunny section. Images (course and category thumbnails) live in a Bunny
/// STORAGE zone, which is separate from the Stream video library: Stream only hosts videos.
///
/// <see cref="AccessKey"/> is the storage zone's password. It is a secret: user-secrets or an
/// environment variable (<c>Storage__Bunny__AccessKey</c>), never appsettings.json, and it must
/// never reach the browser.
/// </summary>
public sealed class BunnyStorageOptions
{
    public const string SectionName = "Storage:Bunny";

    /// <summary>The storage zone name, as it appears in the dashboard.</summary>
    public string StorageZone { get; set; } = string.Empty;

    /// <summary>Storage zone password (its AccessKey).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Regional API host: https://storage.bunnycdn.com (Falkenstein, the default),
    /// https://la.storage.bunnycdn.com, https://sg.storage.bunnycdn.com, https://syd.storage.bunnycdn.com.
    /// Must match the zone's main region.
    /// </summary>
    public string Endpoint { get; set; } = "https://storage.bunnycdn.com";

    /// <summary>Public CDN base the stored path is appended to: the pull zone hostname, with https.</summary>
    public string CdnBaseUrl { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(StorageZone)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(CdnBaseUrl);
}
