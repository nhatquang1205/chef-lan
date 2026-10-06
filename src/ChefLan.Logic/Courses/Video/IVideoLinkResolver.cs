using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Video;

/// <summary>
/// The single place a playable video URL is produced. Lessons must never hand out
/// Lesson.VideoUrl straight from the column.
///
/// An unlisted YouTube or Vimeo link is obscurity, not access control: once a paying
/// customer sees it, they can paste it into a Facebook group and the whole migration is
/// undone. Going through this port means that fix — Vimeo domain-restricted private
/// embeds, or signed short-TTL CDN URLs — is a swap of the implementation, with no change
/// to endpoints, DTOs or schema.
/// </summary>
public interface IVideoLinkResolver
{
    ValueTask<string?> ResolveAsync(Lesson lesson, long customerId, CancellationToken cancellationToken = default);
}
