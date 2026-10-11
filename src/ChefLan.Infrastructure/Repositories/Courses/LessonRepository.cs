using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

/// <summary>
/// ix_lessons_course_id_sort_order is deliberately NOT unique, because a partial unique
/// index cannot be DEFERRABLE in Postgres and a unique one would reject the intermediate
/// states of a drag-and-drop reorder. Uniqueness of sort_order within a course is the
/// reorder use case's job. Do not add a unique index to "fix" it.
/// </summary>
internal sealed class LessonRepository(CoursesDbContext context) : ILessonRepository
{
    public Task<Lesson?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Lesson>> ListForCourseAsync(
        long courseId,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default) =>
        await context.Lessons
            .Where(l => l.CourseId == courseId && (includeUnpublished || l.IsPublished))
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> GetSlugsWithPrefixInCourseAsync(
        long courseId,
        string prefix,
        CancellationToken cancellationToken = default) =>
        (await context.Lessons
            .Where(l => l.CourseId == courseId && l.Slug.StartsWith(prefix))
            .Select(l => l.Slug)
            .ToListAsync(cancellationToken)).ToHashSet();

    public void Add(Lesson lesson) => context.Lessons.Add(lesson);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    public void Remove(Lesson lesson) => context.Lessons.Remove(lesson);
}
