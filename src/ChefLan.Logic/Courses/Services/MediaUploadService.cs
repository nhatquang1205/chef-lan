using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Video;

namespace ChefLan.Logic.Courses.Services;

internal sealed class MediaUploadService(IFileStorage storage, IVideoHost videoHost, IClock clock) : IMediaUploadService
{
    public const int MaxImageBytes = 5 * 1024 * 1024;

    public async Task<Result<UploadedImageResponse>> UploadImageAsync(
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (!storage.IsConfigured)
        {
            return UploadErrors.ImageStorageNotConfigured;
        }

        if (content.Length == 0)
        {
            return UploadErrors.EmptyFile;
        }

        if (content.Length > MaxImageBytes)
        {
            return UploadErrors.ImageTooLarge(MaxImageBytes);
        }

        // The client's filename and Content-Type are untrusted. The bytes decide what this is, and
        // SVG is deliberately not accepted: it can carry script.
        var kind = Sniff(content);

        if (kind is null)
        {
            return UploadErrors.UnsupportedImage;
        }

        var now = clock.UtcNow;
        var path = $"images/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}.{kind.Value.Extension}";

        try
        {
            await using var stream = new MemoryStream(content, writable: false);
            return new UploadedImageResponse(await storage.SaveAsync(path, stream, kind.Value.ContentType, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return UploadErrors.StorageFailed;
        }
    }

    public async Task DeleteImageAsync(string url) => await storage.DeleteByUrlAsync(url);

    public async Task<Result<UploadedVideoResponse>> UploadVideoAsync(
        string title,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!videoHost.IsConfigured)
        {
            return UploadErrors.VideoHostNotConfigured;
        }

        var created = await videoHost.CreateAsync(title, cancellationToken);

        if (created.IsFailure)
        {
            return created.Error;
        }

        var videoId = created.Value;
        Result uploaded;

        try
        {
            uploaded = await videoHost.UploadAsync(videoId, content, cancellationToken);
        }
        catch
        {
            // The client hung up (or the connection broke) mid-upload: that is an exception, not a
            // failed Result. Either way an empty video must not be left behind in the library.
            await videoHost.DeleteAsync(videoId);
            throw;
        }

        if (uploaded.IsFailure)
        {
            await videoHost.DeleteAsync(videoId);
            return uploaded.Error;
        }

        return new UploadedVideoResponse(videoId, title);
    }

    private static (string Extension, string ContentType)? Sniff(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            return ("jpg", "image/jpeg");
        }

        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
        {
            return ("png", "image/png");
        }

        // RIFF....WEBP
        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
        {
            return ("webp", "image/webp");
        }

        return null;
    }
}
