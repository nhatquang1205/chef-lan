namespace ChefLan.Logic.Courses.Contracts.Responses;

/// <summary>Public URL of the stored WebP and its pixel size (lets the web reserve space).</summary>
public sealed record UploadedImageResponse(string Url, int Width, int Height);

/// <summary>
/// <c>VideoId</c> goes into a lesson video's <c>externalId</c>. Bunny encodes after the upload,
/// so playback is not ready for a few minutes (longer for long videos).
/// </summary>
public sealed record UploadedVideoResponse(string VideoId, string Title);
