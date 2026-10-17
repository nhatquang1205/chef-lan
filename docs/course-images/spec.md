# Course images (gallery) and WebP uploads — spec

## Decisions (confirmed by the owner)

| | |
|---|---|
| Cover image | `courses.thumbnail_url` stays as is and is what the **hero slider** shows |
| Gallery | New table `courses.course_images` (ordered). The **course page** shows them as a carousel |
| Course card (lists, "my courses") | **First gallery image** by order; falls back to the cover image |
| No gallery images | Course page and card fall back to the cover image |
| WebP | **Every** image upload is converted on the server before it is stored: cover, gallery, category, video thumbnails. Images uploaded before stay as they are |
| Resize | Longest side **at most 1920 px** (never upscaled), WebP **quality 82**, EXIF stripped (incl. GPS), orientation applied |
| Admin | Multi-upload with per-file progress, drag to reorder, delete (also removes the file from B2), alt text per image |

## Upload flow

```
admin ──(1) POST /api/v1/admins/uploads/images  (one file each, multipart "file")
          server: sniff → decode → auto-orient → resize ≤1920 → encode WebP q82 → B2  → { url, width, height }
      ──(2) POST /api/v1/admins/courses/{id}/images  { url, alt?, width, height }   (attach, JSON)
```
Step 1 goes through the nginx `uploads` location (large bodies allowed, no buffering); step 2 is a small JSON call
under `/api/` (6 MB limit). The same step 1 serves the cover image, category image and video thumbnails, so all of
them become WebP. Accepted input: JPEG, PNG, WebP (sniffed from the bytes; SVG and HEIC refused — iOS converts HEIC to
JPEG when a photo is picked in the browser). **Input limit raised from 5 MB to 15 MB** (phone photos); the output is
typically 150–400 KB.

An image uploaded but never attached stays in B2 (same as today's thumbnails); acceptable for now.

## Data

`courses.course_images`

| column | type | notes |
|---|---|---|
| `id` | bigint identity | PK |
| `course_id` | bigint FK -> courses (RESTRICT) | |
| `url` | varchar(1000) NOT NULL | public URL as returned by the upload |
| `alt` | varchar(200) NULL | SEO / accessibility |
| `width`, `height` | int NULL | from the upload response (lets the web reserve space, no layout shift) |
| `sort_order` | int NOT NULL | 0 = card image |
| audit columns | | as other tables; **no soft delete** (deleting removes the row and the B2 file) |

Index `ix_course_images_course_id_sort_order` (not unique: same rule as lessons, reorder rewrites in one transaction).
Max **20 images per course** (400 `course.too_many_images`). Migration is additive.

## Plans

| # | Repo | Plan | Status |
|---|---|---|---|
| B1 | chef-lan | [WebP pipeline for every image upload](plans/01-webp-pipeline.md) | todo |
| B2 | chef-lan | [Course images: table, admin API, public API](plans/02-course-images.md) | todo |
| W  | chef-lan-web | `docs/plans/03-course-images.md` | todo |
| A  | chef-lan-admin | `docs/plans/17-course-images.md` | todo |

B1 and B2 can be built in either order (B2's attach endpoint only needs a URL); admin needs both; web needs B2.
