using ChefLan.Commons.Money;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

/// <summary>
/// internal sealed: the API cannot resolve this by name, only through
/// <see cref="ICourseCatalogService"/> registered by <see cref="CoursesModule"/>.
/// </summary>
internal sealed class CourseCatalogService(
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    IClock clock) : ICourseCatalogService
{
    public async Task<PagedResult<CourseListItemResponse>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await courses.ListPublishedAsync(query, cancellationToken);

        return PagedResult<CourseListItemResponse>.Create(
            page.Items.Select(c => c.ToListItemResponse()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<Result<CourseDetailResponse>> GetPublishedBySlugAsync(
        string slug,
        long? customerId,
        CancellationToken cancellationToken = default)
    {
        var course = await courses.GetPublishedBySlugAsync(slug, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(slug);
        }

        var outline = course.Lessons.Select(ToOutline).ToList();

        var summary = course.ToListItemResponse();

        return new CourseDetailResponse
        {
            Id = course.Id,
            Slug = course.Slug,
            Title = course.Title,
            Subtitle = course.Subtitle,
            Description = course.Description,
            WhatYouWillLearn = course.WhatYouWillLearn,
            MetaDescription = course.MetaDescription,
            ThumbnailUrl = course.ThumbnailUrl,
            PromoVideoUrl = course.PromoVideoUrl,
            Level = course.Level,
            PublishedAt = course.PublishedAt,
            PriceVnd = course.PriceVnd,
            CompareAtPriceVnd = course.CompareAtPriceVnd,
            PriceDisplay = Vnd.ToDisplay(course.PriceVnd),
            AccessDurationDays = course.AccessDurationDays,
            LessonCount = course.LessonCount,
            TotalDurationSeconds = course.TotalDurationSeconds,
            Categories = summary.Categories,
            Lessons = outline,
        };
    }

    public async Task<Result<LessonPageResponse>> GetPublishedLessonAsync(
        string courseSlug,
        string lessonSlug,
        long? customerId,
        CancellationToken cancellationToken = default)
    {
        var course = await courses.GetPublishedBySlugAsync(courseSlug, cancellationToken);

        if (course is null)
        {
            return CourseErrors.NotFound(courseSlug);
        }

        var lessons = course.Lessons.ToList();
        var index = lessons.FindIndex(l => l.Slug == lessonSlug);

        if (index < 0)
        {
            return LessonErrors.NotFound(courseSlug, lessonSlug);
        }

        var lesson = lessons[index];
        var hasAccess = customerId is { } id
            && await enrollments.HasAccessAsync(id, course.Id, lesson.Id, clock.UtcNow, cancellationToken);
        var hasRecipes = !string.IsNullOrWhiteSpace(lesson.Recipes);

        return new LessonPageResponse
        {
            CourseSlug = course.Slug,
            CourseTitle = course.Title,
            Lesson = ToOutline(lesson),
            HasAccess = hasAccess,
            Recipes = hasAccess && hasRecipes ? lesson.Recipes : null,
            RecipesLocked = hasRecipes && !hasAccess,
            Previous = index > 0 ? new LessonLinkResponse(lessons[index - 1].Slug, lessons[index - 1].Title) : null,
            Next = index < lessons.Count - 1 ? new LessonLinkResponse(lessons[index + 1].Slug, lessons[index + 1].Title) : null,
        };
    }

    // The outline carries no playable links: a link is minted per play by the playback endpoint, which
    // is also where entitlement is decided. Free videos are identified by IsFree / FreeTillSeconds.
    private static LessonOutlineResponse ToOutline(Lesson lesson) => new()
    {
        Id = lesson.Id,
        Slug = lesson.Slug,
        Title = lesson.Title,
        Description = lesson.Description,
        SortOrder = lesson.SortOrder,
        DurationSeconds = lesson.DurationSeconds,
        IncludedInCourse = lesson.IncludedInCourse,
        PriceVnd = lesson.PriceVnd,
        PriceDisplay = lesson.PriceVnd is { } price ? Vnd.ToDisplay(price) : null,
        Videos = lesson.Videos.Select(video => new LessonVideoOutlineResponse
        {
            PublicId = video.PublicId,
            Title = video.Title,
            Description = video.Description,
            ThumbnailUrl = video.ThumbnailUrl,
            DurationSeconds = video.DurationSeconds,
            IsFree = video.IsFree,
            FreeTillSeconds = video.IsFree ? video.FreeTillSeconds : null,
        }).ToList(),
    };
}
