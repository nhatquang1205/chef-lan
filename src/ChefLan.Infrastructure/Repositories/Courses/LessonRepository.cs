using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Courses;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the admin lesson endpoints (step 2).
///
/// When implementing: ix_lessons_course_id_sort_order is deliberately NOT unique, because
/// a partial unique index cannot be DEFERRABLE in Postgres and a unique one would reject
/// the intermediate states of a drag-and-drop reorder. So uniqueness of sort_order within
/// a course is this layer's job, not the database's: the reorder use case must rewrite
/// every row of the course inside one transaction. Do not add a unique index to "fix" it.
///
/// NextSortOrderAsync should return max(sort_order) + 1 over live rows, and 0 for a course
/// with no lessons.
/// </summary>
internal sealed class LessonRepository(CoursesDbContext context) : ILessonRepository
{
    public Task<Lesson?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LessonRepository.GetByIdAsync — step 2 (admin lessons).");

    public Task<Lesson?> GetBySlugAsync(
        long courseId,
        string slug,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LessonRepository.GetBySlugAsync — step 2 (lesson player).");

    public Task<IReadOnlyList<Lesson>> ListForCourseAsync(
        long courseId,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LessonRepository.ListForCourseAsync — step 2.");

    public Task<bool> SlugExistsInCourseAsync(
        long courseId,
        string slug,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LessonRepository.SlugExistsInCourseAsync — step 2.");

    public Task<int> NextSortOrderAsync(long courseId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LessonRepository.NextSortOrderAsync — step 2.");

    public void Add(Lesson lesson) => context.Lessons.Add(lesson);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    public void Remove(Lesson lesson) => context.Lessons.Remove(lesson);
}
