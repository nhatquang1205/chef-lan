using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class CourseErrors
{
    public static Error NotFound(string slug) =>
        Error.NotFound("course.not_found", $"Course '{slug}' was not found.");

    public static Error SlugAlreadyUsed(string slug) =>
        Error.Conflict("course.slug_already_used", $"The slug '{slug}' is already taken.");

    public static readonly Error NoActiveEnrollment =
        Error.Forbidden("course.no_active_enrollment", "You do not have access to this course.");

    public static readonly Error EnrollmentExpired =
        Error.Forbidden("course.enrollment_expired", "Your access to this course has expired.");
}
