namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record UploadedImageResponse(string Url);

/// <summary>
/// <c>VideoId</c> goes into a lesson video's <c>externalId</c>. Bunny encodes after the upload,
/// so playback is not ready for a few minutes (longer for long videos).
/// </summary>
public sealed record UploadedVideoResponse(string VideoId, string Title);
