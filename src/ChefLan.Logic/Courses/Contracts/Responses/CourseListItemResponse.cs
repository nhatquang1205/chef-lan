using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CourseListItemResponse
{
    public required long Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public string? ThumbnailUrl { get; init; }

    public required CourseLevel Level { get; init; }

    public required CourseStatus Status { get; init; }

    public DateTime? PublishedAt { get; init; }

    public required decimal PriceVnd { get; init; }

    public decimal? CompareAtPriceVnd { get; init; }

    public required string PriceDisplay { get; init; }

    public required int LessonCount { get; init; }

    public required int TotalDurationSeconds { get; init; }

    /// <summary>Null means lifetime access.</summary>
    public int? AccessDurationDays { get; init; }

    public required IReadOnlyList<CategoryResponse> Categories { get; init; }
}
