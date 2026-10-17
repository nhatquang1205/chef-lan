using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record CourseIncludeResponse
{
    public required long IncludedCourseId { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required CourseStatus Status { get; init; }

    public required decimal PriceVnd { get; init; }

    public required int LessonCount { get; init; }

    public required decimal DiscountVnd { get; init; }

    public required int SortOrder { get; init; }
}

public sealed record CourseIncludedInResponse
{
    public required long Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }
}
