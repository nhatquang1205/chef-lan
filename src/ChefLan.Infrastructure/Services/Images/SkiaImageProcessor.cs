using ChefLan.Commons.Results;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Errors;
using SkiaSharp;

namespace ChefLan.Infrastructure.Services.Images;

internal sealed class SkiaImageProcessor : IImageProcessor
{
    private readonly ImageProcessingOptions _options = ImageProcessingOptions.Default;

    public Result<ProcessedImage> ToWebp(byte[] input)
    {
        try
        {
            using var data = SKData.CreateCopy(input);
            using var codec = SKCodec.Create(data);

            if (codec is null)
            {
                return UploadErrors.UnsupportedImage;
            }

            var info = codec.Info;

            if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > _options.MaxInputPixels)
            {
                return UploadErrors.UnsupportedImage;
            }

            using var bitmap = SKBitmap.Decode(codec);

            if (bitmap is null)
            {
                return UploadErrors.UnsupportedImage;
            }

            var origin = codec.EncodedOrigin;
            var swapped = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var orientedWidth = swapped ? bitmap.Height : bitmap.Width;
            var orientedHeight = swapped ? bitmap.Width : bitmap.Height;

            var scale = Math.Min(1f, (float)_options.MaxLongestSide / Math.Max(orientedWidth, orientedHeight));
            var width = Math.Max(1, (int)Math.Round(orientedWidth * scale));
            var height = Math.Max(1, (int)Math.Round(orientedHeight * scale));

            using var image = SKImage.FromBitmap(bitmap);
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));

            if (surface is null)
            {
                return UploadErrors.UnsupportedImage;
            }

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)width / orientedWidth, (float)height / orientedHeight);
            var orientation = OrientationMatrix(origin, bitmap.Width, bitmap.Height);
            canvas.Concat(in orientation);
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));

            using var snapshot = surface.Snapshot();
            using var encoded = snapshot.Encode(SKEncodedImageFormat.Webp, _options.Quality);

            if (encoded is null)
            {
                return UploadErrors.UnsupportedImage;
            }

            return new ProcessedImage(encoded.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Deliberately no image bytes (or file names) in the log.
            return UploadErrors.UnsupportedImage;
        }
    }

    /// <summary>Maps source pixels to upright pixels for an EXIF orientation (w, h = source size).</summary>
    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, int w, int h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
