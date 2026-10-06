namespace ChefLan.Infrastructure.Services.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute path on disk that uploads are written under.</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>Public URL prefix the stored path is appended to.</summary>
    public string PublicBaseUrl { get; set; } = "/media";

    public long MaxBytes { get; set; } = 20 * 1024 * 1024;
}
