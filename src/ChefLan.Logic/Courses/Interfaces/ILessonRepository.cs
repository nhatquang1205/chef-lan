using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Lesson?> GetBySlugAsync(long courseId, string slug, CancellationToken cancellationToken = default);

    /// <summary>Ordered by sort_order. Published lessons only unless includeUnpublished.</summary>
    Task<IReadOnlyList<Lesson>> ListForCourseAsync(
        long courseId,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsInCourseAsync(long courseId, string slug, CancellationToken cancellationToken = default);

    Task<int> NextSortOrderAsync(long courseId, CancellationToken cancellationToken = default);

    void Add(Lesson lesson);

    void Remove(Lesson lesson);
}
