using System.Security.Cryptography;
using System.Text;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Video;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Video;

/// <summary>
/// Mints a short-lived signed Bunny Stream embed URL per request.
///
/// Signing formula, from Bunny's docs:
///     token   = SHA256_HEX(TokenSecurityKey + videoId + expiresUnixSeconds)
///     url     = {EmbedBaseUrl}/embed/{libraryId}/{videoId}?token={token}&expires={expires}
///
/// What this does and does not buy you: the URL is TIME-bound, not viewer-bound — Bunny's
/// embed token formula has no field for a customer id. So a copied link works for anyone
/// until it expires, which is why <see cref="BunnyStreamOptions.TokenLifetimeSeconds"/>
/// matters and why the library should also have a referer allowlist set in the Bunny
/// dashboard. Neither stops a screen recording; the threat being addressed is a link
/// pasted into a group chat, which this makes useless within hours.
///
/// customerId is still taken and logged, so access is attributable after the fact.
/// </summary>
internal sealed class BunnyVideoLinkResolver(
    IOptions<BunnyStreamOptions> options,
    IClock clock,
    ILogger<BunnyVideoLinkResolver> logger) : IVideoLinkResolver
{
    private readonly BunnyStreamOptions _options = options.Value;

    public ValueTask<string?> ResolveAsync(
        Lesson lesson,
        long customerId,
        CancellationToken cancellationToken = default)
    {
        // Lessons hosted elsewhere (a free YouTube preview, say) are returned untouched.
        if (lesson.VideoProvider != VideoProvider.Bunny)
        {
            return ValueTask.FromResult(lesson.VideoUrl);
        }

        if (string.IsNullOrWhiteSpace(lesson.VideoExternalId))
        {
            // Fail closed: a Bunny lesson with no video GUID is a data error, and handing
            // back VideoUrl here could leak an unsigned URL.
            logger.LogError(
                "Lesson {LessonId} is marked VideoProvider.Bunny but has no VideoExternalId.",
                lesson.Id);

            return ValueTask.FromResult<string?>(null);
        }

        var videoId = lesson.VideoExternalId;
        var expires = new DateTimeOffset(clock.UtcNow).ToUnixTimeSeconds() + _options.TokenLifetimeSeconds;
        var token = Sign(videoId, expires);

        logger.LogDebug(
            "Signed Bunny playback for lesson {LessonId} / customer {CustomerId}, expires {Expires}.",
            lesson.Id,
            customerId,
            expires);

        var url =
            $"{_options.EmbedBaseUrl.TrimEnd('/')}/embed/{_options.LibraryId}/{videoId}"
            + $"?token={token}&expires={expires}";

        return ValueTask.FromResult<string?>(url);
    }

    private string Sign(string videoId, long expires)
    {
        var payload = $"{_options.TokenSecurityKey}{videoId}{expires}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        // Bunny expects lowercase hex.
        return Convert.ToHexStringLower(hash);
    }
}
