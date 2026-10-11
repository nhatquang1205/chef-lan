using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class LessonVideoRepository(CoursesDbContext context) : ILessonVideoRepository
{
    public Task<LessonVideo?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.LessonVideos.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LessonVideo>> ListForLessonAsync(
        long lessonId,
        CancellationToken cancellationToken = default) =>
        await context.LessonVideos
            .Where(v => v.LessonId == lessonId)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> CourseHasPlayableVideoAsync(long courseId, CancellationToken cancellationToken = default) =>
        context.LessonVideos.AnyAsync(
            v => v.IsPublished
                && (v.ExternalId != null || v.Url != null)
                && v.Lesson!.CourseId == courseId
                && v.Lesson.IsPublished
                && !v.Lesson.IsDeleted,
            cancellationToken);

    public void Add(LessonVideo video) => context.LessonVideos.Add(video);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    public void Remove(LessonVideo video) => context.LessonVideos.Remove(video);
}
