using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Commons.Text;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class CourseService(
    ICourseRepository courses,
    ICategoryRepository categories,
    ILessonRepository lessons,
    ILessonVideoRepository videos,
    ICourseIncludeRepository includes,
    ICourseImageRepository images,
    IUnitOfWork unitOfWork,
    ISlugGenerator slugGenerator,
    IClock clock) : ICourseService
{
    private const int SlugMaxLength = 160;

    public async Task<PagedResult<CourseListItemResponse>> ListAsync(
        AdminCourseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await courses.ListAsync(query, cancellationToken);

        return PagedResult<CourseListItemResponse>.Create(
            page.Items.Select(c => c.ToListItemResponse()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<Result<CourseAdminDetailResponse>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var course = await courses.GetByIdWithCategoriesAsync(id, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(id);
        }

        return course.ToAdminDetailResponse(
            await includes.CountIncludesAsync(id, cancellationToken),
            await includes.CountIncludedInAsync(id, cancellationToken),
            await images.CountAsync(id, cancellationToken));
    }

    public async Task<Result<CourseAdminDetailResponse>> CreateAsync(
        CourseRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveCategoriesAsync(request.CategoryIds, cancellationToken);

        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        var title = request.Title.Trim();
        var slug = await SlugAllocator.AllocateAsync(
            slugGenerator, title, "course", SlugMaxLength, courses.GetSlugsWithPrefixAsync, cancellationToken);

        var course = new Course { Slug = slug, Title = title, Status = CourseStatus.Draft };
        Apply(course, request);

        foreach (var category in resolved.Value)
        {
            course.CourseCategories.Add(new CourseCategory { Category = category });
        }

        courses.Add(course);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return course.ToAdminDetailResponse();
    }

    /// <summary>
    /// Status is deliberately not updated here: a PUT that round-trips the whole object would
    /// let a client with stale data silently publish a course. The slug stays stable too.
    /// </summary>
    public async Task<Result<CourseAdminDetailResponse>> UpdateAsync(
        long id,
        CourseRequest request,
        CancellationToken cancellationToken = default)
    {
        var course = await courses.GetByIdWithCategoriesAsync(id, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(id);
        }

        var resolved = await ResolveCategoriesAsync(request.CategoryIds, cancellationToken);

        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        Apply(course, request);

        var wanted = resolved.Value.Select(c => c.Id).ToHashSet();

        foreach (var link in course.CourseCategories.Where(cc => !wanted.Contains(cc.CategoryId)).ToList())
        {
            course.CourseCategories.Remove(link);
        }

        var existing = course.CourseCategories.Select(cc => cc.CategoryId).ToHashSet();

        foreach (var category in resolved.Value.Where(c => !existing.Contains(c.Id)))
        {
            course.CourseCategories.Add(new CourseCategory { Category = category });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return course.ToAdminDetailResponse(
            await includes.CountIncludesAsync(id, cancellationToken),
            await includes.CountIncludedInAsync(id, cancellationToken),
            await images.CountAsync(id, cancellationToken));
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var course = await courses.GetByIdAsync(id, cancellationToken);

        if (course is null)
        {
            return Result.Failure(CourseErrors.NotFound(id));
        }

        if (await includes.CountIncludesAsync(id, cancellationToken) > 0
            || await includes.CountIncludedInAsync(id, cancellationToken) > 0)
        {
            return Result.Failure(CourseErrors.HasIncludes);
        }

        courses.Remove(course);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> PublishAsync(long id, CancellationToken cancellationToken = default)
    {
        var course = await courses.GetByIdAsync(id, cancellationToken);

        if (course is null)
        {
            return Result.Failure(CourseErrors.NotFound(id));
        }

        if (course.Status == CourseStatus.Published)
        {
            return Result.Success();
        }

        if (!(await lessons.ListForCourseAsync(id, includeUnpublished: false, cancellationToken)).Any(l => l.IncludedInCourse))
        {
            return Result.Failure(CourseErrors.NoPublishedLesson);
        }

        if (!await videos.CourseHasPlayableVideoAsync(id, cancellationToken))
        {
            return Result.Failure(CourseErrors.NoPlayableVideo);
        }

        course.Status = CourseStatus.Published;
        course.PublishedAt ??= clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>Enrollments are untouched: an unpublished course still plays for people who bought it.</summary>
    public async Task<Result> UnpublishAsync(long id, CancellationToken cancellationToken = default)
    {
        var course = await courses.GetByIdAsync(id, cancellationToken);

        if (course is null)
        {
            return Result.Failure(CourseErrors.NotFound(id));
        }

        if (course.Status == CourseStatus.Published)
        {
            course.Status = CourseStatus.Draft;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result<IReadOnlyList<Category>>> ResolveCategoriesAsync(
        IReadOnlyList<long>? ids,
        CancellationToken cancellationToken)
    {
        var distinct = ids?.Distinct().ToList() ?? [];

        if (distinct.Count == 0)
        {
            return Result.Success<IReadOnlyList<Category>>([]);
        }

        var found = await categories.GetByIdsAsync(distinct, cancellationToken);
        var missing = distinct.Except(found.Select(c => c.Id)).ToList();

        if (missing.Count > 0)
        {
            return CourseErrors.UnknownCategories(missing);
        }

        return Result.Success<IReadOnlyList<Category>>(found);
    }

    private static void Apply(Course course, CourseRequest request)
    {
        course.Title = request.Title.Trim();
        course.Subtitle = SlugAllocator.Clean(request.Subtitle);
        course.Description = SlugAllocator.Clean(request.Description);
        course.WhatYouWillLearn = SlugAllocator.Clean(request.WhatYouWillLearn);
        course.MetaDescription = SlugAllocator.Clean(request.MetaDescription);
        course.ThumbnailUrl = SlugAllocator.Clean(request.ThumbnailUrl);
        course.PromoVideoUrl = SlugAllocator.Clean(request.PromoVideoUrl);
        course.Level = request.Level;
        course.PriceVnd = request.PriceVnd;
        course.CompareAtPriceVnd = request.CompareAtPriceVnd;
        course.AccessDurationDays = request.AccessDurationDays;
        course.LegacyFacebookGroupUrl = SlugAllocator.Clean(request.LegacyFacebookGroupUrl);
    }
}
