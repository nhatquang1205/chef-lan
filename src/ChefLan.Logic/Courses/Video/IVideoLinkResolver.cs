using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Video;

/// <summary>
/// The single place a playable video URL is produced. Nothing may hand out
/// <see cref="LessonVideo.Url"/> or <see cref="LessonVideo.ExternalId"/> straight from the
/// column.
///
/// An unlisted YouTube or Vimeo link is obscurity, not access control: once a paying
/// customer sees it, they can paste it into a Facebook group and the whole migration is
/// undone. Going through this port means the fix — signed short-TTL URLs, or domain
/// restricted embeds — is a swap of the implementation, with no change to endpoints, DTOs
/// or schema.
/// </summary>
public interface IVideoLinkResolver
{
    /// <summary>
    /// Returns a playable URL, or <c>null</c> when the caller is not entitled to one.
    ///
    /// <para>
    /// <paramref name="hasFullAccess"/> is granted by an active, unexpired enrollment.
    /// Without it, only <see cref="LessonVideo.IsFree"/> videos resolve:
    /// </para>
    /// <list type="table">
    ///   <item><term>hasFullAccess</term><description>signed URL</description></item>
    ///   <item><term>free video</term><description>signed URL; the frontend honours FreeTillSeconds</description></item>
    ///   <item><term>neither</term><description>null</description></item>
    /// </list>
    ///
    /// <para>
    /// <paramref name="customerId"/> is null for anonymous callers. It is used for logging
    /// and attribution, not for the decision.
    /// </para>
    /// </summary>
    ValueTask<string?> ResolveAsync(
        LessonVideo video,
        bool hasFullAccess,
        long? customerId,
        CancellationToken cancellationToken = default);
}
