using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Requests;

/// <summary>
/// Create and update share one shape. Slug, status, publishedAt, currency and the counters
/// are server-owned and have no property here; unknown JSON fields are ignored, so a client
/// that sends them changes nothing.
/// </summary>
public sealed record CourseRequest(
    string Title,
    string? Subtitle,
    string? Description,
    string? WhatYouWillLearn,
    string? MetaDescription,
    string? ThumbnailUrl,
    string? PromoVideoUrl,
    decimal PriceVnd,
    decimal? CompareAtPriceVnd,
    int? AccessDurationDays,
    string? LegacyFacebookGroupUrl,
    IReadOnlyList<long>? CategoryIds,
    CourseLevel Level = CourseLevel.Unspecified);

/// <summary>The admin list: everything the public list supports, plus drafts and a status filter.</summary>
public sealed record AdminCourseListQuery : CourseListQuery
{
    public CourseStatus? Status { get; init; }
}
