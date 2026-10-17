using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ICourseIncludeRepository
{
    /// <summary>Tracked, with the included course loaded, ordered by sort_order then id.</summary>
    Task<IReadOnlyList<CourseInclude>> ListForCourseAsync(long courseId, CancellationToken cancellationToken = default);

    /// <summary>Tracked.</summary>
    Task<CourseInclude?> GetAsync(long courseId, long includedCourseId, CancellationToken cancellationToken = default);

    /// <summary>The live courses that include this one, by title.</summary>
    Task<IReadOnlyList<Course>> ListParentsAsync(long includedCourseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only: the PUBLISHED courses this one includes, in sort order, each with its published
    /// included-in-course lessons and their published videos.
    /// </summary>
    Task<IReadOnlyList<Course>> ListPublishedIncludedWithLessonsAsync(long courseId, CancellationToken cancellationToken = default);

    /// <summary>Read-only: the published courses that include this one, by title.</summary>
    Task<IReadOnlyList<Course>> ListPublishedParentsAsync(long includedCourseId, CancellationToken cancellationToken = default);

    Task<int> CountIncludesAsync(long courseId, CancellationToken cancellationToken = default);

    Task<int> CountIncludedInAsync(long courseId, CancellationToken cancellationToken = default);

    void Add(CourseInclude include);

    void Remove(CourseInclude include);
}
