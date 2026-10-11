using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record LessonAdminListItemResponse
{
    public required long Id { get; init; }

    public required long CourseId { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsPublished { get; init; }

    /// <summary>Price to buy this lesson alone; null = not sold separately.</summary>
    public decimal? PriceVnd { get; init; }

    public required int VideoCount { get; init; }

    public required int DurationSeconds { get; init; }
}

public sealed record LessonAdminDetailResponse
{
    public required long Id { get; init; }

    public required long CourseId { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>Markdown, stored and returned unaltered.</summary>
    public string? Recipes { get; init; }

    public string? ResourcesUrl { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsPublished { get; init; }

    /// <summary>Price to buy this lesson alone; null = not sold separately.</summary>
    public decimal? PriceVnd { get; init; }

    public required int VideoCount { get; init; }

    public required int DurationSeconds { get; init; }
}

public sealed record LessonVideoAdminResponse
{
    public required long Id { get; init; }

    public required long LessonId { get; init; }

    /// <summary>The opaque id the public site uses for this video.</summary>
    public required string PublicId { get; init; }

    public required int SortOrder { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required VideoProvider Provider { get; init; }

    public string? ExternalId { get; init; }

    public string? Url { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required int DurationSeconds { get; init; }

    public required bool IsFree { get; init; }

    public int? FreeTillSeconds { get; init; }

    public required bool IsPublished { get; init; }
}
