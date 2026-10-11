using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ChefLan.Commons.Results;
using ChefLan.Logic.Courses.Video;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Video;

/// <summary>
/// Bunny Stream management API: POST /library/{id}/videos {title} creates the record and returns
/// its guid; PUT /library/{id}/videos/{guid} with the raw bytes uploads the file. Both need the
/// library API key as the AccessKey header — it never leaves this class.
///
/// The upload body is a <see cref="StreamContent"/> over the incoming request stream, so it is
/// forwarded as it arrives: no whole-file buffering in memory or on disk. The HttpClient has no
/// timeout (a big file on a slow connection takes a long time); the request's cancellation token
/// is what ends it.
/// </summary>
internal sealed class BunnyVideoHost(
    HttpClient http,
    IOptions<BunnyStreamOptions> options,
    ILogger<BunnyVideoHost> logger) : IVideoHost
{
    private readonly BunnyStreamOptions _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.LibraryId) && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<Result<string>> CreateAsync(string title, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"library/{_options.LibraryId}/videos")
        {
            Content = JsonContent.Create(new { title }),
        };
        request.Headers.Add("AccessKey", _options.ApiKey);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync("create", response, cancellationToken);
            }

            var created = await response.Content.ReadFromJsonAsync<CreatedVideo>(cancellationToken);

            return string.IsNullOrWhiteSpace(created?.Guid)
                ? Error.Upstream("upload.video_host_failed", "The video host returned no video id.")
                : created.Guid;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach Bunny Stream to create a video.");
            return Error.Upstream("upload.video_host_failed", "The video host could not be reached.");
        }
    }

    public async Task<Result> UploadAsync(string videoId, Stream content, CancellationToken cancellationToken = default)
    {
        using var body = new StreamContent(content, 81920);
        body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"library/{_options.LibraryId}/videos/{Uri.EscapeDataString(videoId)}")
        {
            Content = body,
        };
        request.Headers.Add("AccessKey", _options.ApiKey);

        try
        {
            // ResponseHeadersRead: do not wait to buffer a response body we hardly need.
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure((await FailAsync("upload", response, cancellationToken)).Error);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not upload to Bunny Stream.");
            return Result.Failure(Error.Upstream("upload.video_host_failed", "The video host could not be reached."));
        }
    }

    public async Task DeleteAsync(string videoId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"library/{_options.LibraryId}/videos/{Uri.EscapeDataString(videoId)}");
            request.Headers.Add("AccessKey", _options.ApiKey);

            using var response = await http.SendAsync(request, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Could not clean up Bunny video {VideoId}: {Status}.", videoId, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clean up Bunny video {VideoId}.", videoId);
        }
    }

    private async Task<Result<string>> FailAsync(string step, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        logger.LogError(
            "Bunny Stream {Step} failed: {Status} {Body}",
            step,
            (int)response.StatusCode,
            await response.Content.ReadAsStringAsync(cancellationToken));

        return Error.Upstream("upload.video_host_failed", $"The video host rejected the {step} ({(int)response.StatusCode}).");
    }

    private sealed record CreatedVideo([property: JsonPropertyName("guid")] string? Guid);
}
