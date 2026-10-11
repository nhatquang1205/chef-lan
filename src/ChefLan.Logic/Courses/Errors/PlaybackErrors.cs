using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class PlaybackErrors
{
    public static readonly Error NotFound =
        Error.NotFound("video.not_found", "This video was not found.");

    public static readonly Error NotFree =
        Error.Forbidden("video.not_free", "This video is not free. Enroll in the course to watch it.");

    // A free, published video whose source is missing or unusable (e.g. a Bunny video with no id).
    public static readonly Error NotPlayable =
        Error.NotFound("video.not_playable", "This video cannot be played right now.");
}
