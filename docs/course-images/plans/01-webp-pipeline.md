# B1 — Convert every uploaded image to WebP

Read `../spec.md` and `CLAUDE.md` (uploads section: images go through `MediaUploadService.UploadImageAsync`, bytes are
sniffed, SVG refused).

## Build

1. **Port** `ChefLan.Logic/Abstractions/IImageProcessor.cs`:
   `Result<ProcessedImage> ToWebp(byte[] input)` → `ProcessedImage(byte[] Content, int Width, int Height)`.
   Errors: `upload.unsupported_image` (cannot decode).
2. **Adapter** `ChefLan.Infrastructure/Services/Images/SkiaImageProcessor.cs` with **SkiaSharp** (MIT) +
   **SkiaSharp.NativeAssets.Linux.NoDependencies** (the runtime image is Debian `aspnet:10.0`; NoDependencies avoids
   installing fontconfig). Versions in `Directory.Packages.props`, pick releases at least 2 weeks old.
   - `SKCodec.Create` → read `EncodedOrigin`, decode, apply the orientation (rotate/flip) so phone photos are upright.
   - If the longest side > 1920: scale down proportionally with high-quality sampling (`SKSamplingOptions` with
     cubic/mitchell or `SKFilterQuality.High` equivalent in the chosen version). Never upscale.
   - Encode `SKEncodedImageFormat.Webp`, quality 82. Re-encoding drops EXIF/GPS. Keep alpha for PNGs.
   - Constants (1920, 82) in one place (`ImageProcessingOptions` with defaults; not config yet).
3. **`MediaUploadService.UploadImageAsync`**: after the size and sniff checks, run `ToWebp`, store as
   `images/{yyyy}/{MM}/{guid}.webp` with `image/webp`. `MaxImageBytes` → **15 MB** (input). Response
   `UploadedImageResponse` gains `Width`, `Height` (of the stored WebP).
   - The course create/update **multipart** path (`thumbnailFile`) uses the same service, so it converts too; it is still
     limited by nginx's 6 MB on `/api/` (document it; the admin will upload covers via the uploads endpoint instead).
   - Update the controller's `RequestSizeLimit` on the image endpoint for 15 MB + framing.
4. **Dockerfile**: verify the native library loads in the runtime image (a one-off container run that uploads an image,
   or a startup self-check log line). Add packages to the image only if strictly needed.
5. Docs: `CLAUDE.md` uploads paragraph (WebP, 15 MB input, 1920/82, EXIF stripped, HEIC refused).

## Done when (local; delete uploaded test files after if B2 is configured, otherwise expect 503)

- Unit-level check (a small test or a dev-only console run, not committed): a 4000×3000 JPEG with EXIF orientation 6
  becomes an upright 1440×1920 WebP; a 800×600 PNG with transparency stays 800×600 with alpha; a 1.2 MB phone photo
  ends up well under 1 MB; a text file → `upload.unsupported_image`; SVG refused.
- Through the API with B2 configured: the returned URL ends in `.webp`, `Content-Type: image/webp`, width/height in
  the response. Without B2: 503 as before.
- `docker build` + run the image and upload once: works on Linux (native lib present).
- Release build `-warnaserror` clean. Status + implementation notes.
