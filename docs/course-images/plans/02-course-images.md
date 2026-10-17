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
