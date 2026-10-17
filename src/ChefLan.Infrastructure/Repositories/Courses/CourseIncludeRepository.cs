using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class CourseIncludeRepository(CoursesDbContext context) : ICourseIncludeRepository
{
    public async Task<IReadOnlyList<CourseInclude>> ListForCourseAsync(
        long courseId,
        CancellationToken cancellationToken = default) =>
        await context.CourseIncludes
            .Include(i => i.IncludedCourse)
            .Where(i => i.CourseId == courseId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.IncludedCourseId)
            .ToListAsync(cancellationToken);

    public Task<CourseInclude?> GetAsync(
        long courseId,
        long includedCourseId,
        CancellationToken cancellationToken = default) =>
        context.CourseIncludes
            .Include(i => i.IncludedCourse)
            .FirstOrDefaultAsync(
                i => i.CourseId == courseId && i.IncludedCourseId == includedCourseId,
                cancellationToken);

    public async Task<IReadOnlyList<Course>> ListParentsAsync(
        long includedCourseId,
        CancellationToken cancellationToken = default) =>
        await context.CourseIncludes
            .AsNoTracking()
            .Where(i => i.IncludedCourseId == includedCourseId)
            .Select(i => i.Course!)
            .OrderBy(c => c.Title)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public Task<int> CountIncludesAsync(long courseId, CancellationToken cancellationToken = default) =>
        context.CourseIncludes.CountAsync(i => i.CourseId == courseId, cancellationToken);

    public Task<int> CountIncludedInAsync(long courseId, CancellationToken cancellationToken = default) =>
        context.CourseIncludes.CountAsync(i => i.IncludedCourseId == courseId, cancellationToken);

    public void Add(CourseInclude include) => context.CourseIncludes.Add(include);

    public void Remove(CourseInclude include) => context.CourseIncludes.Remove(include);
}
