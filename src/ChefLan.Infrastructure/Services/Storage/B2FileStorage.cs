using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ChefLan.Logic.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Storage;

/// <summary>
/// Backblaze B2 through its S3-compatible API. Objects go in a public bucket under unguessable keys
/// (the caller builds the key from a random id, never from client input) and the returned URL is a
/// plain link: nothing to sign, nothing to expire.
/// </summary>
internal sealed class B2FileStorage : IFileStorage, IDisposable
{
    private readonly B2StorageOptions _options;
    private readonly ILogger<B2FileStorage> _logger;
    private readonly AmazonS3Client? _client;

    public B2FileStorage(IOptions<B2StorageOptions> options, ILogger<B2FileStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.IsConfigured)
        {
            return;
        }

        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.KeyId, _options.AppKey),
            new AmazonS3Config
            {
                ServiceURL = _options.Endpoint,
                AuthenticationRegion = _options.ResolvedRegion,
                ForcePathStyle = true,
                // Newer AWS SDKs add CRC checksum trailers by default; S3-compatible stores such as B2
                // reject or mishandle them. Only send what the protocol requires.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    public bool IsConfigured => _client is not null;

    public async Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var key = ValidateKey(path);

        try
        {
            await Client().PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = _options.Bucket,
                    Key = key,
                    InputStream = content,
                    ContentType = contentType,
                    // B2 does not support aws-chunked streaming signatures; sign the whole payload.
                    UseChunkEncoding = false,
                    // Keys are unique per upload, so the object never needs revalidating.
                    Headers = { CacheControl = "public, max-age=31536000, immutable" },
                },
                cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            _logger.LogError(ex, "B2 rejected {Key}: {Status} {Code}", key, (int)ex.StatusCode, ex.ErrorCode);
            throw new HttpRequestException($"B2 returned {(int)ex.StatusCode} ({ex.ErrorCode}).", ex);
        }
        catch (AmazonClientException ex)
        {
            _logger.LogError(ex, "Could not reach B2 for {Key}.", key);
            throw new HttpRequestException("B2 could not be reached.", ex);
        }

        return $"{_options.ResolvedPublicBaseUrl}/{key}";
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            // On a versioned B2 bucket this hides the file (a delete marker); a bucket lifecycle rule
            // ("keep only the last version") makes B2 reclaim the space.
            await Client().DeleteObjectAsync(_options.Bucket, ValidateKey(path), cancellationToken);
            return true;
        }
        catch (AmazonServiceException ex)
        {
            _logger.LogWarning(ex, "B2 could not delete {Key}: {Status}", path, (int)ex.StatusCode);
            return false;
        }
    }

    public async Task<bool> DeleteByUrlAsync(string publicUrl)
    {
        var prefix = _options.ResolvedPublicBaseUrl + "/";

        if (_client is null || !publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
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
            _logger.LogWarning(ex, "Could not clean up stored file {Url}.", publicUrl);
            return false;
        }
    }

    public void Dispose() => _client?.Dispose();

    private AmazonS3Client Client() =>
        _client ?? throw new InvalidOperationException("Image storage is not configured.");

    /// <summary>Relative, no traversal, no odd characters. Defence in depth: callers build the key.</summary>
    private static string ValidateKey(string path)
    {
        var segments = path.Split('/');

        if (path.StartsWith('/')
            || segments.Any(s => s is "" or "." or ".."
                || !s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
        {
            throw new ArgumentException("Invalid storage key.", nameof(path));
        }

        return path;
    }
}
