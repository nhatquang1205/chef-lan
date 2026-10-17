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

    public static readonly Error IncludeSelf =
        Error.Validation("course.include_self", "A course cannot include itself.");

    public static readonly Error IncludeNested =
        Error.Validation(
            "course.include_nested",
            "Only one level is allowed: a course that is included elsewhere cannot include others, "
            + "and an included course cannot include others.");

    public static readonly Error IncludeDiscountTooHigh =
        Error.Validation(
            "course.include_discount_too_high",
            "The discount cannot be higher than the course price.");

    public static Error IncludeNotFound(long courseId, long includedCourseId) =>
        Error.NotFound(
            "course.include_not_found",
            $"Course {courseId} does not include course {includedCourseId}.");

    public static readonly Error HasIncludes =
        Error.Conflict(
            "course.has_includes",
            "The course includes other courses or is included by one. Remove those links first.");
}
