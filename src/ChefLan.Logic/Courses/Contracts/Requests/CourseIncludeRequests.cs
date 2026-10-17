namespace ChefLan.Logic.Courses.Contracts.Requests;

public sealed record CourseIncludeRequest(decimal DiscountVnd);

public sealed record CourseIncludeOrderRequest(IReadOnlyList<long> IncludedCourseIds);
