using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Video;

namespace ChefLan.Logic.Courses.Services;

internal sealed class PlaybackService(
    ILessonVideoRepository videos,
    IEnrollmentRepository enrollments,
    IVideoLinkResolver links,
    IClock clock) : IPlaybackService
{
    public async Task<Result<PlaybackResponse>> GetAsync(
        string publicId,
        long? customerId,
        bool viewerIsStaff,
        CancellationToken cancellationToken = default)
    {
        var video = await videos.GetPublicByPublicIdAsync(publicId, cancellationToken);

        if (video is null)
        {
            return PlaybackErrors.NotFound;
        }

        // Entitlement before anything is minted. Staff (signed in on the website with their staff login) see
        // everything published, like owners. Owners (an active course enrollment, or an active
        // enrollment for this lesson) watch everything, unlimited. Everyone else gets only free videos,
        // and the resolver enforces the same rule a second time.
        var now = clock.UtcNow;
        var hasFullAccess = viewerIsStaff
            || (customerId is { } id
                && await enrollments.HasAccessAsync(id, video.Lesson!.CourseId, video.LessonId, now, cancellationToken));

        if (!video.IsFree && !hasFullAccess)
        {
            return PlaybackErrors.NotFree;
        }

        var url = await links.ResolveAsync(video, hasFullAccess, customerId, cancellationToken);

        if (url is null)
        {
            return PlaybackErrors.NotPlayable;
        }

        return new PlaybackResponse
        {
            VideoId = video.PublicId,
            Title = video.Title,
            Description = video.Description,
            Url = url,
            ExpiresAt = links.LinkLifetime is { } lifetime ? now + lifetime : null,
            IsFree = video.IsFree,
            FreeTillSeconds = hasFullAccess ? null : video.FreeTillSeconds,
            HasFullAccess = hasFullAccess,
            DurationSeconds = video.DurationSeconds,
            CourseSlug = video.Lesson!.Course!.Slug,
            CourseTitle = video.Lesson.Course.Title,
            LessonTitle = video.Lesson.Title,
            LessonSlug = video.Lesson.Slug,
        };
    }
}
