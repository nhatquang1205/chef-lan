using ChefLan.Commons.Pagination;
using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Requests;

public record CourseListQuery : PagedRequest
{
    /// <summary>Free-text match on title and subtitle.</summary>
    public string? Q { get; init; }

    public string? CategorySlug { get; init; }

    public CourseLevel? Level { get; init; }

    public CourseSortOrder Sort { get; init; } = CourseSortOrder.Newest;
}
