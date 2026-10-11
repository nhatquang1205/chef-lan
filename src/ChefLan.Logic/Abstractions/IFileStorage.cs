namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Port for binary assets: course thumbnails, lesson resource PDFs. Returns the public URL
/// to persist on the entity.
/// </summary>
public interface IFileStorage
{
    /// <summary>False when no storage backend is configured; uploads then answer 503.</summary>
    bool IsConfigured { get; }

    Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best effort and never throws: deletes the file a public URL (as returned by
    /// <see cref="SaveAsync"/>) points at. A URL that is not ours (a thumbnail pasted from elsewhere)
    /// is left alone and returns false.
    /// </summary>
    Task<bool> DeleteByUrlAsync(string publicUrl);
}
