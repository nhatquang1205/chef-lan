using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ILessonRepository
{
    /// <summary>Tracked.</summary>
    Task<Lesson?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked and ordered by sort_order then id. Use cases rely on the tracked instances
    /// to recompute course counters inside the same SaveChanges as the write.
    /// </summary>
    Task<IReadOnlyList<Lesson>> ListForCourseAsync(
        long courseId,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default);

    /// <summary>Slugs of live lessons in this course that start with <paramref name="prefix"/>.</summary>
    Task<IReadOnlySet<string>> GetSlugsWithPrefixInCourseAsync(
        long courseId,
        string prefix,
        CancellationToken cancellationToken = default);

    void Add(Lesson lesson);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    void Remove(Lesson lesson);
}
