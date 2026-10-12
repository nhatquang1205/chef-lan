using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Requests;

public sealed record LessonRequest(
    string Title,
    string? Description,
    string? Recipes,
    string? ResourcesUrl,
    bool IsPublished = true,
    decimal? PriceVnd = null,
    bool IncludedInCourse = true);

public sealed record LessonOrderRequest(IReadOnlyList<long> LessonIds);

public sealed record LessonVideoRequest(
    string Title,
    string? Description,
    string? ExternalId,
    string? Url,
    string? ThumbnailUrl,
    int DurationSeconds,
    bool IsFree,
    int? FreeTillSeconds,
    VideoProvider Provider = VideoProvider.Bunny,
    bool IsPublished = true);

public sealed record LessonVideoOrderRequest(IReadOnlyList<long> VideoIds);
