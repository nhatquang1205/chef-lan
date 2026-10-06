namespace ChefLan.Logic.Abstractions;

/// <summary>
/// Port for binary assets: course thumbnails, lesson resource PDFs. Returns the public URL
/// to persist on the entity.
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
}
