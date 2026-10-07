namespace ChefLan.Infrastructure.Services.Video;

/// <summary>
/// Bound from the Video:Bunny configuration section.
///
/// Two DIFFERENT Bunny secrets are involved and they are easy to mix up:
///  - <see cref="TokenSecurityKey"/> signs playback URLs. Library -> Security -> enable
///    "Embed View Token Authentication".
///  - <see cref="ApiKey"/> is the Stream library API key, used as the AccessKey header for
///    REST calls (create video, upload, delete). It must NEVER reach the browser.
///
/// Both belong in user-secrets or environment variables, never appsettings.json.
/// </summary>
public sealed class BunnyStreamOptions
{
    public const string SectionName = "Video:Bunny";

    /// <summary>Numeric video library id from the Bunny dashboard.</summary>
    public string LibraryId { get; set; } = string.Empty;

    /// <summary>Signs embed URLs. Not the API key.</summary>
    public string TokenSecurityKey { get; set; } = string.Empty;

    /// <summary>AccessKey header for the Stream REST API. Server-side only.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string EmbedBaseUrl { get; set; } = "https://iframe.mediadelivery.net";

    /// <summary>
    /// How long a playback URL stays valid. This is the window in which a copied link
    /// still works for someone else, so keep it short — long enough to watch one lesson,
    /// not long enough to be worth pasting into a group chat. The player re-requests on
    /// reload, so a short value costs the viewer nothing.
    /// </summary>
    public int TokenLifetimeSeconds { get; set; } = 7200;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(LibraryId) && !string.IsNullOrWhiteSpace(TokenSecurityKey);
}
