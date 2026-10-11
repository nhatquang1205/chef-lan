using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class LessonErrors
{
    public static Error NotFound(long id) =>
        Error.NotFound("lesson.not_found", $"Lesson {id} was not found.");

    public static Error VideoNotFound(long id) =>
        Error.NotFound("lesson_video.not_found", $"Lesson video {id} was not found.");

    public static readonly Error OrderMismatch =
        Error.Validation(
            "order.mismatch",
            "The list must contain every current item exactly once, and nothing else.");
}
