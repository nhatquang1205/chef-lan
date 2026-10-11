using System.Net.Http.Headers;
using ChefLan.Logic.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Storage;

/// <summary>
/// Bunny Storage: PUT {endpoint}/{zone}/{path} with the zone password as the AccessKey header and
/// the raw bytes as the body; 201 on success. The path is built by the caller from a random id,
/// never from client input.
/// </summary>
internal sealed class BunnyFileStorage(
    HttpClient http,
    IOptions<BunnyStorageOptions> options,
    ILogger<BunnyFileStorage> logger) : IFileStorage
{
    private readonly BunnyStorageOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var clean = ValidatePath(path);

        using var body = new StreamContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        if (content.CanSeek)
        {
            body.Headers.ContentLength = content.Length;
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{Uri.EscapeDataString(_options.StorageZone)}/{clean}")
        {
            Content = body,
        };
        request.Headers.Add("AccessKey", _options.AccessKey);

        using var response = await http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Bunny Storage rejected {Path}: {Status} {Body}",
                clean,
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new HttpRequestException($"Bunny Storage returned {(int)response.StatusCode}.");
        }

        return $"{_options.CdnBaseUrl.TrimEnd('/')}/{clean}";
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var clean = ValidatePath(path);

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{Uri.EscapeDataString(_options.StorageZone)}/{clean}");
        request.Headers.Add("AccessKey", _options.AccessKey);

        using var response = await http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteByUrlAsync(string publicUrl)
    {
        var prefix = _options.CdnBaseUrl.TrimEnd('/') + "/";

        if (!_options.IsConfigured || !publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await DeleteAsync(publicUrl[prefix.Length..], timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clean up stored file {Url}.", publicUrl);
            return false;
        }
    }

    /// <summary>Relative, no traversal, no odd characters. Defence in depth: callers build the path.</summary>
    private static string ValidatePath(string path)
    {
        var segments = path.Split('/');

        if (path.StartsWith('/')
            || segments.Any(s => s is "" or "." or ".."
                || !s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
        {
            throw new ArgumentException("Invalid storage path.", nameof(path));
        }

        return path;
    }
}
