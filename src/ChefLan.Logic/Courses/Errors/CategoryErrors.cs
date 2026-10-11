using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class CategoryErrors
{
    public static Error NotFound(long id) =>
        Error.NotFound("category.not_found", $"Category {id} was not found.");

    public static readonly Error InUse =
        Error.Conflict(
            "category.in_use",
            "This category still has courses. Set its status to InActive to hide it from the "
            + "storefront without breaking those courses, or move the courses out first.");
}
