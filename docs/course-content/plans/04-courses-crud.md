# Plan 4 — Courses CRUD

**Status: done.** Publish 409s with a distinct code for each missing piece (`course.no_published_lesson`, `course.no_playable_video`). Publishing an already published course, or unpublishing a draft, is a 204 no-op. The slug is stable on rename. Needs a staff token. Copies the pattern from
[plan 3](03-categories-crud.md). See [spec.md](../spec.md) for context.

**Goal:** 7 admin + 1 new customer endpoint.

---

## Endpoints

```
GET    /api/v1/courses/{slug}                -> CourseDetailResponse   anonymous
GET    /api/v1/admins/courses?status=&q=&categorySlug=&page=&pageSize=&sort=
                                             -> PagedResult<CourseListItemResponse>  incl. drafts
GET    /api/v1/admins/courses/{id}           -> CourseAdminDetailResponse
POST   /api/v1/admins/courses                -> 201 CourseAdminDetailResponse
PUT    /api/v1/admins/courses/{id}           -> CourseAdminDetailResponse
DELETE /api/v1/admins/courses/{id}           -> 204                    soft delete
POST   /api/v1/admins/courses/{id}/publish   -> 204
POST   /api/v1/admins/courses/{id}/unpublish -> 204
```

Body: `title`, `subtitle?`, `description?`, `whatYouWillLearn?`, `metaDescription?`,
`thumbnailUrl?`, `promoVideoUrl?`, `level`, `priceVnd`, `compareAtPriceVnd?`,
`accessDurationDays?`, `legacyFacebookGroupUrl?`, `categoryIds?`.

**Server-owned, never accepted from the client:** `slug` (generated from `title`), `status`
(publish/unpublish only), `publishedAt`, `currency`, `lessonCount`, `totalDurationSeconds`,
`enrollmentCount`.

`status` is deliberately not a PUT field. A PUT that round-trips the whole object would let a
client with stale data silently publish a course.

## Rules

- **Publish refuses `409`** when the course has no published lesson with a playable video.
  Until plan 5 exists there are no lessons at all, so publish always 409s — that **is** this
  plan's test, and plan 5 flips it to 204.
- `publishedAt` is set on **first** publish only; re-publishing after an unpublish keeps the
  original date, so the storefront's "newest" ordering stays stable.
- Unpublish → `Draft`. Enrollments are untouched either way — an unpublished course still
  plays for people who bought it.
- `priceVnd` validated with `Vnd.IsValid`. Postgres `numeric(12,0)` **rounds** a fractional
  amount rather than rejecting it, so the check has to happen in the application layer or
  `1200000.50` silently becomes `1200001`.
- `compareAtPriceVnd` must be ≥ `priceVnd` (the DB CHECK agrees, but a 400 beats a 500).
- `categoryIds` replaces the whole set on PUT. Unknown or soft-deleted ids → 400.
- `CourseDetailResponse` carries the lesson outline but **no video URLs**; plan 6 adds them.

## Files

- `ChefLan.Logic/Courses/Interfaces/ICourseRepository.cs` — extend: admin list including
  drafts, get by id, add, slug existence
- `ChefLan.Logic/Courses/Interfaces/ICourseService.cs`
- `ChefLan.Logic/Courses/Queries/CourseQueryExtensions.cs` — add `WithStatus`
- `ChefLan.Logic/Courses/Contracts/`, `Validators/`, `Mapping/CourseMappings.cs` — extend
- `ChefLan.Infrastructure/Repositories/Courses/CourseRepository.cs` — extend
- `ChefLan.Courses.API/Controllers/Admin/AdminCoursesController.cs`;
  extend `Controllers/CoursesController.cs` with the detail route

## Done when

- `POST` → `Draft`, slug generated from title, `categoryIds` attached
- `POST` with a Vietnamese title → correct slug (`Khóa học bánh mì` → `khoa-hoc-banh-mi`)
- `publish` with no lessons → **409**
- `priceVnd = 1200000.50` → **400** from `Vnd.IsValid`, not silently rounded to `1200001`
- `priceVnd = -1` → 400
- `compareAtPriceVnd` below `priceVnd` → 400
- `PUT` with a `status` field in the body → the field is ignored, status unchanged
- admin list shows drafts; `GET /api/v1/courses` does not
- `GET /api/v1/courses/{unknown-slug}` → 404 `ProblemDetails`
- `GET /api/v1/courses/{draft-slug}` → 404, not 200 — drafts are not reachable by slug
- `DELETE` → soft; gone from both lists; slug reusable
- customer token on any `/admins/*` route → 401
