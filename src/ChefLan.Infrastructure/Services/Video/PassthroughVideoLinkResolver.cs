using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Video;

namespace ChefLan.Infrastructure.Services.Video;

/// <summary>
/// Local-development fallback, used when Video:Bunny is not configured. Hands back the
/// stored URL as-is.
///
/// **Not safe for production** — an unsigned URL is a permanent shareable link. It still
/// honours entitlement, so a non-free video resolves to null rather than leaking.
/// </summary>
public sealed class PassthroughVideoLinkResolver : IVideoLinkResolver
{
    public ValueTask<string?> ResolveAsync(
        LessonVideo video,
        bool hasFullAccess,
        long? customerId,
        CancellationToken cancellationToken = default) =>
        hasFullAccess || video.IsFree
            ? ValueTask.FromResult(video.Url)
            : ValueTask.FromResult<string?>(null);
}
