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

    void Add(LessonVideo video);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    void Remove(LessonVideo video);
}
