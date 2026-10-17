using ChefLan.Commons.Results;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

public interface IPlaybackService
{
    /// <summary>
    /// The playable link and context for one video, or why not: 404 when the video, its lesson or its
    /// course is not public; 403 when it is not free (full access arrives with enrollments).
    /// </summary>
    Task<Result<PlaybackResponse>> GetAsync(
        string publicId,
        long? customerId,
        bool viewerIsStaff,
        CancellationToken cancellationToken = default);
}
