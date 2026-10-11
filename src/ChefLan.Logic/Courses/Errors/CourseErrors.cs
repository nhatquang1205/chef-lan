using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class CourseErrors
{
    public static Error NotFound(string slug) =>
        Error.NotFound("course.not_found", $"Course '{slug}' was not found.");

    public static Error NotFound(long id) =>
        Error.NotFound("course.not_found", $"Course {id} was not found.");

    public static Error SlugAlreadyUsed(string slug) =>
        Error.Conflict("course.slug_already_used", $"The slug '{slug}' is already taken.");

    public static readonly Error NoPublishedLesson =
        Error.Conflict(
            "course.no_published_lesson",
            "The course cannot be published: it has no published lesson. Add a lesson first.");

    public static readonly Error NoPlayableVideo =
        Error.Conflict(
            "course.no_playable_video",
            "The course cannot be published: none of its published lessons has a published video "
            + "with a source. Add a video to a lesson first.");

    public static Error UnknownCategories(IEnumerable<long> ids) =>
        Error.Validation(
            "course.unknown_categories",
            $"These category ids do not exist or were deleted: {string.Join(", ", ids)}.");

    public static readonly Error NoActiveEnrollment =
        Error.Forbidden("course.no_active_enrollment", "You do not have access to this course.");

    public static readonly Error EnrollmentExpired =
        Error.Forbidden("course.enrollment_expired", "Your access to this course has expired.");
}
