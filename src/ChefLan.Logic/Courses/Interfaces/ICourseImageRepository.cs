using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ICourseImageRepository
{
    /// <summary>Tracked.</summary>
    Task<CourseImage?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Tracked, ordered by sort_order then id.</summary>
    Task<IReadOnlyList<CourseImage>> ListForCourseAsync(long courseId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(long courseId, CancellationToken cancellationToken = default);

    void Add(CourseImage image);

    /// <summary>Hard delete: the table has no soft delete.</summary>
    void Remove(CourseImage image);
}
