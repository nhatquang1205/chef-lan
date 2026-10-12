using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CourseAdminDetailResponse
{
    public required long Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public string? Description { get; init; }

    public string? WhatYouWillLearn { get; init; }

    public string? MetaDescription { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? PromoVideoUrl { get; init; }

    public required CourseLevel Level { get; init; }

    public required CourseStatus Status { get; init; }

    public DateTime? PublishedAt { get; init; }

    public required decimal PriceVnd { get; init; }

    public decimal? CompareAtPriceVnd { get; init; }

    public required string Currency { get; init; }

    public int? AccessDurationDays { get; init; }

    public required int LessonCount { get; init; }

    public required int TotalDurationSeconds { get; init; }

    public required int EnrollmentCount { get; init; }

    public string? LegacyFacebookGroupUrl { get; init; }

    public required IReadOnlyList<CategoryResponse> Categories { get; init; }
}

/// <summary>The public course page: outline plus the videos a visitor may watch.</summary>
public sealed record CourseDetailResponse
{
    public required long Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public string? Description { get; init; }

    public string? WhatYouWillLearn { get; init; }

    public string? MetaDescription { get; init; }

    public string? ThumbnailUrl { get; init; }

    public string? PromoVideoUrl { get; init; }

    public required CourseLevel Level { get; init; }

    public DateTime? PublishedAt { get; init; }

    public required decimal PriceVnd { get; init; }

    public decimal? CompareAtPriceVnd { get; init; }

    public required string PriceDisplay { get; init; }

    public int? AccessDurationDays { get; init; }

    public required int LessonCount { get; init; }

    public required int TotalDurationSeconds { get; init; }

    public required IReadOnlyList<CategoryResponse> Categories { get; init; }

    public required IReadOnlyList<LessonOutlineResponse> Lessons { get; init; }
}

/// <summary>Recipes and resources are deliberately absent: they are part of what is sold.</summary>
public sealed record LessonOutlineResponse
{
    public required long Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required int SortOrder { get; init; }

    public required int DurationSeconds { get; init; }

    /// <summary>What this lesson costs on its own; null = only available through the course.</summary>
    public decimal? PriceVnd { get; init; }

    public string? PriceDisplay { get; init; }

    /// <summary>False = add-on: not covered by the course price, bought separately.</summary>
    public required bool IncludedInCourse { get; init; }

    public required IReadOnlyList<LessonVideoOutlineResponse> Videos { get; init; }
}

public sealed record LessonVideoOutlineResponse
{
    /// <summary>Opaque public id: what the watch page URL uses. The integer id is never public.</summary>
    public required string PublicId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required int DurationSeconds { get; init; }

    public required bool IsFree { get; init; }

    /// <summary>
    /// Enforced by the FRONTEND: null means the whole video is free. The URL is for the full
    /// video, so this is a teaser limit, not a paywall.
    /// </summary>
    public int? FreeTillSeconds { get; init; }

    // No URL here. A playable link is minted fresh, when someone opens the watch page, by
    // GET /api/v1/videos/{publicId}/playback — which is also where entitlement is decided.
}
