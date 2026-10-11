using ChefLan.Commons.Money;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
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
internal sealed class CourseCatalogService(ICourseRepository courses) : ICourseCatalogService
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

        // The outline carries no playable links: a link is minted per play by the playback endpoint, which
        // is also where entitlement is decided. Free videos are identified by IsFree / FreeTillSeconds.
        var outline = course.Lessons.Select(lesson => new LessonOutlineResponse
        {
            Id = lesson.Id,
            Slug = lesson.Slug,
            Title = lesson.Title,
            Description = lesson.Description,
            SortOrder = lesson.SortOrder,
            DurationSeconds = lesson.DurationSeconds,
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
        }).ToList();

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
}
