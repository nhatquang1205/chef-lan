using ChefLan.Commons.Money;
using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;
using ChefLan.Logic.Courses.Video;

namespace ChefLan.Logic.Courses.Services;

/// <summary>
/// internal sealed: the API cannot resolve this by name, only through
/// <see cref="ICourseCatalogService"/> registered by <see cref="CoursesModule"/>.
/// </summary>
internal sealed class CourseCatalogService(ICourseRepository courses, IVideoLinkResolver videoLinks)
    : ICourseCatalogService
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

        var outline = new List<LessonOutlineResponse>();

        foreach (var lesson in course.Lessons)
        {
            var videoOutline = new List<LessonVideoOutlineResponse>();

            foreach (var video in lesson.Videos)
            {
                // Entitlement is the resolver's decision, not ours: with no enrollment gating
                // yet, hasFullAccess is always false, so only free videos get a URL.
                var url = await videoLinks.ResolveAsync(
                    video, hasFullAccess: false, customerId, cancellationToken);

                videoOutline.Add(new LessonVideoOutlineResponse
                {
                    Id = video.Id,
                    Title = video.Title,
                    Description = video.Description,
                    ThumbnailUrl = video.ThumbnailUrl,
                    DurationSeconds = video.DurationSeconds,
                    IsFree = video.IsFree,
                    FreeTillSeconds = video.IsFree ? video.FreeTillSeconds : null,
                    Url = url,
                });
            }

            outline.Add(new LessonOutlineResponse
            {
                Id = lesson.Id,
                Slug = lesson.Slug,
                Title = lesson.Title,
                Description = lesson.Description,
                SortOrder = lesson.SortOrder,
                DurationSeconds = lesson.DurationSeconds,
                Videos = videoOutline,
            });
        }

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
