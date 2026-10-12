using ChefLan.Commons.Results;
using ChefLan.Commons.Text;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class LessonService(
    ILessonRepository lessons,
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ISlugGenerator slugGenerator) : ILessonService
{
    private const int SlugMaxLength = 160;

    public async Task<Result<IReadOnlyList<LessonAdminListItemResponse>>> ListAsync(
        long courseId,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var list = await lessons.ListForCourseAsync(courseId, includeUnpublished: true, cancellationToken);

        return Result.Success<IReadOnlyList<LessonAdminListItemResponse>>(
            list.Select(l => l.ToListItemResponse()).ToList());
    }

    public async Task<Result<LessonAdminDetailResponse>> CreateAsync(
        long courseId,
        LessonRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await courses.GetByIdAsync(courseId, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var existing = await lessons.ListForCourseAsync(courseId, includeUnpublished: true, cancellationToken);
        var title = request.Title.Trim();
        var slug = await SlugAllocator.AllocateAsync(
            slugGenerator,
            title,
            "lesson",
            SlugMaxLength,
            (prefix, ct) => lessons.GetSlugsWithPrefixInCourseAsync(courseId, prefix, ct),
            cancellationToken);

        var lesson = new Lesson
        {
            CourseId = courseId,
            Slug = slug,
            Title = title,
            SortOrder = existing.Count == 0 ? 0 : existing.Max(l => l.SortOrder) + 1,
        };

        Apply(lesson, request);
        lessons.Add(lesson);

        Counters.Apply(course, existing.Append(lesson));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return lesson.ToDetailResponse();
    }

    public async Task<Result<LessonAdminDetailResponse>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var lesson = await lessons.GetByIdAsync(id, cancellationToken);

        return lesson is null ? LessonErrors.NotFound(id) : lesson.ToDetailResponse();
    }

    public async Task<Result<LessonAdminDetailResponse>> UpdateAsync(
        long id,
        LessonRequest request,
        CancellationToken cancellationToken = default)
    {
        var lesson = await lessons.GetByIdAsync(id, cancellationToken);

        if (lesson is null)
        {
            return LessonErrors.NotFound(id);
        }

        Apply(lesson, request);

        var course = await courses.GetByIdAsync(lesson.CourseId, cancellationToken);

        if (course is not null)
        {
            Counters.Apply(
                course,
                await lessons.ListForCourseAsync(lesson.CourseId, includeUnpublished: true, cancellationToken));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return lesson.ToDetailResponse();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var lesson = await lessons.GetByIdAsync(id, cancellationToken);

        if (lesson is null)
        {
            return Result.Failure(LessonErrors.NotFound(id));
        }

        var course = await courses.GetByIdAsync(lesson.CourseId, cancellationToken);
        var all = await lessons.ListForCourseAsync(lesson.CourseId, includeUnpublished: true, cancellationToken);

        lessons.Remove(lesson);

        if (course is not null)
        {
            Counters.Apply(course, all.Where(l => l.Id != id));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// ix_lessons_course_id_sort_order is deliberately not unique, so uniqueness of the order
    /// is enforced here: the list must be exactly the course's live lessons, and every row is
    /// rewritten in one SaveChanges.
    /// </summary>
    public async Task<Result> ReorderAsync(
        long courseId,
        LessonOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await courses.GetByIdAsync(courseId, cancellationToken) is null)
        {
            return Result.Failure(CourseErrors.NotFound(courseId));
        }

        var all = await lessons.ListForCourseAsync(courseId, includeUnpublished: true, cancellationToken);

        if (!Counters.IsExactOrdering(request.LessonIds, all.Select(l => l.Id).ToList()))
        {
            return Result.Failure(LessonErrors.OrderMismatch);
        }

        var byId = all.ToDictionary(l => l.Id);

        for (var position = 0; position < request.LessonIds.Count; position++)
        {
            byId[request.LessonIds[position]].SortOrder = position;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static void Apply(Lesson lesson, LessonRequest request)
    {
        lesson.Title = request.Title.Trim();
        lesson.Description = SlugAllocator.Clean(request.Description);
        lesson.Recipes = request.Recipes;
        lesson.ResourcesUrl = SlugAllocator.Clean(request.ResourcesUrl);
        lesson.IsPublished = request.IsPublished;
        lesson.PriceVnd = request.PriceVnd;
        lesson.IncludedInCourse = request.IncludedInCourse;
    }
}
