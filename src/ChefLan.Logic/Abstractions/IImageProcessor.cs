using ChefLan.Commons.Results;

namespace ChefLan.Logic.Abstractions;

/// <summary>The re-encoded image and its pixel size (after orientation and resizing).</summary>
public sealed record ProcessedImage(byte[] Content, int Width, int Height);

/// <summary>Defaults for every uploaded image. Constants for now, not configuration.</summary>
public sealed record ImageProcessingOptions(int MaxLongestSide = 1920, int Quality = 82, long MaxInputPixels = 100_000_000)
{
    public static readonly ImageProcessingOptions Default = new();
}

/// <summary>
/// Port for turning an uploaded JPEG/PNG/WebP into the WebP we store: orientation applied, longest side
/// capped (never upscaled), metadata (EXIF, GPS) dropped. <c>upload.unsupported_image</c> when the bytes
/// cannot be decoded.
/// </summary>
public interface IImageProcessor
{
    Result<ProcessedImage> ToWebp(byte[] input);
}
