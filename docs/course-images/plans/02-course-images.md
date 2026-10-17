# B2 — Course images: table, admin API, public API

Read `../spec.md` and `CLAUDE.md`. Patterns to copy: lesson videos (ordered children, reorder with the full id list,
`Counters.IsExactOrdering`), `IFileStorage.DeleteByUrlAsync` (best effort, ignores foreign URLs).

## Schema

Entity `CourseImage`, configuration, `Course.Images`. Migration `AddCourseImages` (additive). No soft delete.

## Admin API (StaffOrAdmin)

```
GET    /api/v1/admins/courses/{id}/images                -> CourseImageResponse[]  (ordered)
POST   /api/v1/admins/courses/{id}/images                -> 201 CourseImageResponse   body { url, alt?, width?, height? }  (appended last)
PUT    /api/v1/admins/course-images/{imageId}            -> CourseImageResponse       body { alt? }
DELETE /api/v1/admins/course-images/{imageId}            -> 204  (row deleted, then the B2 file best effort)
PUT    /api/v1/admins/courses/{id}/images/order          -> 204  body { imageIds: [...] }  (exact set, else 400 order.mismatch)
```
`CourseImageResponse { id, url, alt?, width?, height?, sortOrder }`. Validation: `url` absolute http(s) ≤ 1000;
`alt` ≤ 200; width/height positive when given; max 20 per course (`course.too_many_images`); unknown course/image 404
(`course.not_found`, `course.image_not_found`). `CourseAdminDetailResponse` gains `imageCount`.
Deleting a course: its images stay (course is soft-deleted) — no change.

## Public API

- `CourseListItemResponse` (public list, admin list, my-courses if it reuses it): `cardImageUrl` = first image by
  `sort_order`, else `thumbnailUrl`. Compute in the list query (one correlated subquery / projection; no N+1).
  `MyCourseResponse` (GET /me/courses) gets the same field.
- `CourseDetailResponse`: `images: [{ url, alt?, width?, height? }]` in order (raw; empty when none — the web falls back
  to `thumbnailUrl`).
- `thumbnailUrl` unchanged everywhere (hero uses it).

## Done when (local DB; clean up after)

Attach 3 images (any URLs) to a course → ordered; reorder; edit alt; delete one (row gone; B2 delete attempted only for
our URLs); 21st image → 400. Public list shows `cardImageUrl` = first image; a course without images → its thumbnail;
detail returns `images` in order. Release build clean; migration in `make script`. Status + implementation notes.

## Implementation notes

Status: done (local DB, curl-verified; migration `AddCourseImages` applied locally and present in `make script`).

- **Migration**: additive, one table `courses.course_images` (id identity, course_id FK RESTRICT, url varchar(1000), alt varchar(200) null,
  width/height int null, sort_order int, audit columns, CHECKs: sort_order >= 0, width/height null or > 0) plus
  `ix_course_images_course_id_sort_order` (not unique). No soft delete. Code: `CourseImage` entity + `CourseImageConfiguration`,
  `Course.Images`, `ICourseImageRepository`, `ICourseImageService` (in `IContentServices.cs`) / `CourseImageService`, `AdminCourseImagesController`.
- **Admin API** (StaffOrAdmin), all as in the plan:
  - `GET /api/v1/admins/courses/{id}/images` -> `[{ id, url, alt?, width?, height?, sortOrder }]` ordered.
  - `POST /api/v1/admins/courses/{id}/images` body `{ url, alt?, width?, height? }` -> 201 `CourseImageResponse` (appended last).
  - `PUT /api/v1/admins/course-images/{imageId}` body `{ alt? }` -> 200 `CourseImageResponse` (null/blank alt clears it).
  - `DELETE /api/v1/admins/course-images/{imageId}` -> 204; row deleted first, then `IFileStorage.DeleteByUrlAsync` (best effort, foreign URLs ignored).
  - `PUT /api/v1/admins/courses/{id}/images/order` body `{ "imageIds": [...] }` -> 204; not exactly the current ids -> 400 `order.mismatch`.
  - Errors: 404 `course.not_found` / `course.image_not_found`; 400 `course.too_many_images` (limit 20); url not absolute http(s) or > 1000, alt > 200, width/height <= 0 -> 400 `ValidationProblemDetails`.
- **`CourseAdminDetailResponse.imageCount`** (int; GET and PUT return the real count, POST create returns 0 as it has none).
- **`cardImageUrl`** (string?, = first image by sort order, else `thumbnailUrl`) on `CourseListItemResponse` (public list, admin list, and the summary used
  by detail) and on `MyCourseResponse` (`GET /me/courses`). Computed with a filtered include `Images.OrderBy(...).Take(1)` in the same query (no N+1).
- **`CourseDetailResponse.images`**: `[{ url, alt?, width?, height? }]` in order, `[]` when none. `thumbnailUrl` unchanged everywhere.
- **Checks passed** (curl against the local API on :5061): attach 3 (ordered 0,1,2), reorder, bad order 400, alt edit, delete (second delete 404),
  20 images ok and the 21st -> 400 `course.too_many_images`, public/admin list `cardImageUrl` = first image (and the cover for a course with none),
  detail `images` in order / empty, `/me/courses` `cardImageUrl`, unauthenticated 401, unknown course/image 404, bad URL 400. Test rows removed afterwards.
  Not exercised: the B2 file deletion itself (storage not configured locally).
