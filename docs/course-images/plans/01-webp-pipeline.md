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

## Implementation notes

Status: done (local and Docker verified; B2 itself not exercised, no storage credentials locally).

- **Port/adapter**: `ChefLan.Logic/Abstractions/IImageProcessor.cs` (also holds `ProcessedImage` and `ImageProcessingOptions`:
  1920 px, quality 82, plus a 100 MP input-pixel cap against decompression bombs). Adapter
  `ChefLan.Infrastructure/Services/Images/SkiaImageProcessor.cs` (internal, singleton). Orientation is applied with an explicit
  matrix per EXIF origin while drawing into the output surface (Mitchell cubic sampling). Any decode failure -> `upload.unsupported_image`;
  nothing image-related is logged.
- **Packages**: `SkiaSharp` 4.153.1 and `SkiaSharp.NativeAssets.Linux.NoDependencies` 4.153.1 (both published 2026-09-30). The macOS native
  ships inside the main package. No extra transitive pin was needed (`-warnaserror` build clean).
- **Upload response**: `{ "url": "https://.../images/2026/10/<guid>.webp", "width": 1440, "height": 1920 }` (stored WebP size).
- **Limits**: input 15 MB (service and `AdminUploadsController`, request limit 16 MB for multipart framing). The sniff (JPEG/PNG/WebP) still
  runs first; SVG/HEIC/text -> 400 `upload.unsupported_image`; storage unconfigured -> 503 `upload.image_storage_not_configured` (checked first).
  The course multipart `thumbnailFile` path goes through the same service but keeps its 6 MB cap.
- **Checks** (throwaway console outside the repo calling the real `SkiaImageProcessor`): 4000x3000 JPEG (3.3 MB) with EXIF orientation 1/2/3/6/8 ->
  1920x1440 (orient 1/2/3) and 1440x1920 (6/8) WebP, ~680 KB, corner colours confirm each orientation is applied, no `EXIF` marker in the output;
  800x600 PNG stays 800x600 (never upscaled); PNG with transparency keeps alpha (corner alpha 0, VP8X present); WebP input re-encodes;
  text and SVG -> `upload.unsupported_image`. API locally: 503 `upload.image_storage_not_configured`.
- **Docker**: `docker build` succeeds; the same check run inside the runtime image (aarch64 Debian `aspnet:10.0`, via `dotnet exec` with the app's deps file)
  loaded `libSkiaSharp.so` and produced identical results. No packages had to be added to the image. Image removed afterwards.
