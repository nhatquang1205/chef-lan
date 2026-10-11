using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Errors;

public static class UploadErrors
{
    public static readonly Error ImageStorageNotConfigured =
        Error.Unavailable("upload.image_storage_not_configured", "Image storage is not configured on this server.");

    public static readonly Error VideoHostNotConfigured =
        Error.Unavailable("upload.video_host_not_configured", "Video hosting is not configured on this server.");

    public static readonly Error NoFile =
        Error.Validation("upload.no_file", "Attach the file as a form field named 'file'.");

    public static readonly Error EmptyFile =
        Error.Validation("upload.empty_file", "The file is empty.");

    public static Error ImageTooLarge(int maxBytes) =>
        Error.Validation("upload.image_too_large", $"Images may be at most {maxBytes / (1024 * 1024)} MB.");

    public static readonly Error UnsupportedImage =
        Error.Validation("upload.unsupported_image", "Only JPEG, PNG and WebP images are accepted.");

    public static readonly Error StorageFailed =
        Error.Upstream("upload.storage_failed", "The image could not be saved to storage. Try again.");
}
