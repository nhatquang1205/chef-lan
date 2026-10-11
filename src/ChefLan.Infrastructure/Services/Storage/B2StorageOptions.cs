namespace ChefLan.Infrastructure.Services.Storage;

/// <summary>
/// Bound from the Storage:B2 section. Images (thumbnails) go to a PUBLIC Backblaze B2 bucket through
/// its S3-compatible API, the same way the wish-list project does; Bunny is used for video only.
///
/// <see cref="AppKey"/> is a secret: environment variable (<c>Storage__B2__AppKey</c>) or user-secrets,
/// never appsettings.json. Scope the B2 application key to this one bucket.
/// </summary>
public sealed class B2StorageOptions
{
    public const string SectionName = "Storage:B2";

    public string Bucket { get; set; } = string.Empty;

    /// <summary>The bucket's S3 endpoint, e.g. https://s3.us-west-004.backblazeb2.com.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Optional: taken from the endpoint (s3.&lt;region&gt;.backblazeb2.com) when empty.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Application key id (B2 calls it keyID).</summary>
    public string KeyId { get; set; } = string.Empty;

    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional: the public base URL objects are served from, with no trailing slash — a custom domain
    /// or CDN in front of the bucket. When empty it is built like wish-list does:
    /// https://&lt;bucket&gt;.s3.&lt;region&gt;.backblazeb2.com
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(KeyId)
        && !string.IsNullOrWhiteSpace(AppKey);

    /// <summary>The region for signing: explicit, else parsed from the endpoint, else B2's default.</summary>
    public string ResolvedRegion
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Region))
            {
                return Region.Trim();
            }

            var host = Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;
            var parts = host.Split('.');

            return parts.Length > 2 && parts[0] == "s3" ? parts[1] : "us-east-005";
        }
    }

    public string ResolvedPublicBaseUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PublicBaseUrl))
            {
                return PublicBaseUrl.TrimEnd('/');
            }

            var endpoint = Endpoint.TrimEnd('/');
            var scheme = endpoint.IndexOf("://", StringComparison.Ordinal);

            return scheme < 0 ? endpoint : $"{endpoint[..(scheme + 3)]}{Bucket}.{endpoint[(scheme + 3)..]}";
        }
    }
}
