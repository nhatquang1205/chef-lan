using ChefLan.Commons.Auth;
using ChefLan.Commons.Results;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>
/// File uploads for staff. Both take multipart/form-data; the files end up on Bunny and the
/// response carries what to store: a CDN URL for an image, a video id for a video.
/// </summary>
[ApiController]
[Route("api/v1/admins/uploads")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminUploadsController(IMediaUploadService uploads) : ControllerBase
{
    private const int MaxImageBytes = 15 * 1024 * 1024;
    private const int MaxImageRequestBytes = MaxImageBytes + 1024 * 1024; // room for multipart framing
    private const long MaxVideoBytes = 8L * 1024 * 1024 * 1024;
    private const int MaxTitleLength = 200;

    /// <summary>
    /// A JPEG, PNG or WebP up to 15 MB; returns the CDN URL of the stored WebP (converted, longest side at most 1920 px, EXIF removed) with its width and height. Two ways to send it:
    /// <list type="bullet">
    ///   <item>the raw image as the request <b>body</b> (Postman: Body -> binary; curl:
    ///   <c>--data-binary @thumb.jpg</c>). The Content-Type header is ignored: the bytes decide.</item>
    ///   <item>multipart/form-data with a form field named <c>file</c>.</item>
    /// </list>
    /// </summary>
    [HttpPost("images")]
    [RequestSizeLimit(MaxImageRequestBytes)]
    [ProducesResponseType(typeof(UploadedImageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadedImageResponse>> UploadImage(CancellationToken cancellationToken)
    {
        var bytes = Request.HasFormContentType
            ? await ReadFormFileAsync(cancellationToken)
            : await ReadBodyAsync(cancellationToken);

        return bytes is null
            ? ProblemFor(Error.Validation("upload.image_too_large", "Images may be at most 15 MB."))
            : (await uploads.UploadImageAsync(bytes, cancellationToken)).ToActionResult(this);
    }

    /// <summary>
    /// Form fields: <c>title</c> (optional, put it BEFORE the file; defaults to the file name) and
    /// <c>file</c>. The file is streamed to Bunny as it arrives, never held in memory or on disk.
    /// The returned <c>videoId</c> goes into a lesson video's <c>externalId</c>.
    /// </summary>
    [HttpPost("videos")]
    [DisableFormValueModelBinding]
    [RequestSizeLimit(MaxVideoBytes)]
    [ProducesResponseType(typeof(UploadedVideoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadedVideoResponse>> UploadVideo(CancellationToken cancellationToken)
    {
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var contentType)
            || !contentType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || HeaderUtilities.RemoveQuotes(contentType.Boundary).Length == 0)
        {
            return ProblemFor(Error.Validation("upload.not_multipart", "Send the request as multipart/form-data."));
        }

        // A slow upload must not be killed by the default minimum-data-rate rule.
        if (HttpContext.Features.Get<IHttpMinRequestBodyDataRateFeature>() is { } rate)
        {
            rate.MinDataRate = null;
        }

        var reader = new MultipartReader(HeaderUtilities.RemoveQuotes(contentType.Boundary).Value!, Request.Body);
        string? title = null;

        while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
        {
            // Both plain fields (title) and the file part are form-data; only the file has a filename.
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
                || !disposition.DispositionType.Equals("form-data"))
            {
                continue;
            }

            var name = HeaderUtilities.RemoveQuotes(disposition.Name).Value;

            if (name == "title")
            {
                using var text = new StreamReader(section.Body, leaveOpen: true);
                var buffer = new char[MaxTitleLength + 1];
                var read = await text.ReadBlockAsync(buffer, cancellationToken);
                title = new string(buffer, 0, Math.Min(read, MaxTitleLength)).Trim();
            }
            else if (name == "file" && disposition.IsFileDisposition())
            {
                var fileName = HeaderUtilities.RemoveQuotes(disposition.FileName).Value;
                var resolvedTitle = !string.IsNullOrWhiteSpace(title)
                    ? title
                    : Path.GetFileNameWithoutExtension(fileName ?? "video");

                // `section.Body` is the live request stream for this part: reading it is the upload.
                return (await uploads.UploadVideoAsync(
                    string.IsNullOrWhiteSpace(resolvedTitle) ? "video" : resolvedTitle,
                    section.Body,
                    cancellationToken)).ToActionResult(this);
            }
        }

        return ProblemFor(Error.Validation("upload.no_file", "Attach the file as a form field named 'file'."));
    }

    /// <summary>The multipart form's "file" field (or its only file). Empty array when there is none.</summary>
    private async Task<byte[]?> ReadFormFileAsync(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"] ?? form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            return [];
        }

        if (file.Length > MaxImageBytes)
        {
            return null;
        }

        await using var stream = file.OpenReadStream();
        var bytes = new byte[file.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return bytes;
    }

    /// <summary>The raw request body, capped at the image limit. Null when it is larger.</summary>
    private async Task<byte[]?> ReadBodyAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentLength > MaxImageBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;

        while ((read = await Request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxImageBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private ActionResult ProblemFor(Error error) => Result.Failure(error).ToActionResult(this);
}
