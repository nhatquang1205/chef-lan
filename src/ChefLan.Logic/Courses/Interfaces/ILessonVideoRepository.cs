using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ILessonVideoRepository
{
    /// <summary>Tracked.</summary>
    Task<LessonVideo?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Tracked, live videos (published or not), ordered by sort_order then id.</summary>
    Task<IReadOnlyList<LessonVideo>> ListForLessonAsync(long lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Does any published lesson of the course hold a published video that has a source
    /// (external id or url)? The publish rule.
    /// </summary>
    Task<bool> CourseHasPlayableVideoAsync(long courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only. A video by its PUBLIC id, with its lesson and course, only when the whole chain is
    /// public: the video, its lesson and its course are published and none is deleted.
    /// </summary>
    Task<LessonVideo?> GetPublicByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);

    void Add(LessonVideo video);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    void Remove(LessonVideo video);
}
