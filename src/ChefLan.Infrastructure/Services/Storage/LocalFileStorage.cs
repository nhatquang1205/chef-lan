using ChefLan.Logic.Abstractions;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Storage;

/// <summary>
/// SKELETON — not implemented yet. Course thumbnails and lesson resource PDFs.
///
/// When implementing: treat the incoming path as untrusted. Reject absolute paths and any
/// segment that escapes the root, and derive the stored name yourself rather than using
/// the client's filename. Local disk is fine for a single host; swap in an object-storage
/// implementation behind this same port when there is more than one.
/// </summary>
internal sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    public Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LocalFileStorage.SaveAsync — step 2+ (media upload).");

    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("LocalFileStorage.DeleteAsync — step 2+ (media upload).");
}
