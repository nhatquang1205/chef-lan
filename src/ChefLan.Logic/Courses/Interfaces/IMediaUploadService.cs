using ChefLan.Commons.Results;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

public interface IMediaUploadService
{
    /// <summary>Validates by file CONTENT (not the client's name or content type), then stores it.</summary>
    Task<Result<UploadedImageResponse>> UploadImageAsync(byte[] content, CancellationToken cancellationToken = default);

    /// <summary>Best effort: removes an image this service uploaded (e.g. its course failed to save).</summary>
    Task DeleteImageAsync(string url);

    Task<Result<UploadedVideoResponse>> UploadVideoAsync(
        string title, Stream content, CancellationToken cancellationToken = default);
}
