using ChefLan.Commons.Results;

namespace ChefLan.Logic.Courses.Video;

/// <summary>
/// Port for the video host's management API (Bunny Stream): create a video record and send the
/// file. Separate from <see cref="IVideoLinkResolver"/>, which only produces playback URLs.
/// </summary>
public interface IVideoHost
{
    /// <summary>False when the host is not configured; uploads then answer 503.</summary>
    bool IsConfigured { get; }

    /// <summary>Creates an empty video and returns its id (the Bunny GUID).</summary>
    Task<Result<string>> CreateAsync(string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams <paramref name="content"/> to the host as the video's file. The stream is read
    /// once and never buffered whole, so a multi-gigabyte file costs no memory or disk here.
    /// </summary>
    Task<Result> UploadAsync(string videoId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Best effort: removes a video whose upload failed. Never throws.</summary>
    Task DeleteAsync(string videoId);
}
