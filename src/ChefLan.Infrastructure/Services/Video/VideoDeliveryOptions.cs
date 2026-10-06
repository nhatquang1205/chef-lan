namespace ChefLan.Infrastructure.Services.Video;

public sealed class VideoDeliveryOptions
{
    public const string SectionName = "VideoDelivery";

    /// <summary>CDN host that serves the signed URLs.</summary>
    public string CdnBaseUrl { get; set; } = string.Empty;

    /// <summary>Signing key. Belongs in user-secrets or an env var, never appsettings.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Keep this short — it is the window in which a leaked URL still works.</summary>
    public int UrlLifetimeSeconds { get; set; } = 300;
}
