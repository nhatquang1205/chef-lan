using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class CourseImageRepository(CoursesDbContext context) : ICourseImageRepository
{
    public Task<CourseImage?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.CourseImages.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CourseImage>> ListForCourseAsync(
        long courseId,
        CancellationToken cancellationToken = default) =>
        await context.CourseImages
            .Where(i => i.CourseId == courseId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(long courseId, CancellationToken cancellationToken = default) =>
        context.CourseImages.CountAsync(i => i.CourseId == courseId, cancellationToken);

    public void Add(CourseImage image) => context.CourseImages.Add(image);

    public void Remove(CourseImage image) => context.CourseImages.Remove(image);
}
