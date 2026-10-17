# B1 — Schema and admin API for included courses

Read `../spec.md` first. Follow `CLAUDE.md` (Clean Architecture rules, one config per entity, `Vnd.IsValid`, Result<T>,
FluentValidation, hand-written mappings, ConflictException on races).

## Schema

- Entity `ChefLan.Domain/Courses/Entities/CourseInclude.cs` (`CourseId`, `IncludedCourseId`, `DiscountVnd`, `SortOrder`,
  navs `Course`, `IncludedCourse`). `Course.Includes` (as parent) and `Course.IncludedIn` (as child) collections.
- `ChefLan.DB/Configurations/Courses/CourseIncludeConfiguration.cs`: table `courses.course_includes`, PK, CHECKs, FKs
  RESTRICT, index `ix_course_includes_included_course_id`. Course soft delete: deleting a course that is included
  anywhere or includes anything → 409 `course.has_includes` (do not cascade silently).
- Enrollment columns from the spec (`list_price_vnd`, `discount_vnd`, `discount_reason`) in the same migration
  `AddCourseIncludes` (all nullable / additive). `make migration NAME=AddCourseIncludes`, review, `make migrate`.

## Admin API (StaffOrAdmin)

```
GET    /api/v1/admins/courses/{id}/includes                      -> CourseIncludeResponse[]  (ordered)
PUT    /api/v1/admins/courses/{id}/includes/{includedCourseId}   -> CourseIncludeResponse     body { discountVnd }  (add or update)
DELETE /api/v1/admins/courses/{id}/includes/{includedCourseId}   -> 204
PUT    /api/v1/admins/courses/{id}/includes/order                -> 204  body { includedCourseIds: [...] } (exact set, else 400 order.mismatch)
GET    /api/v1/admins/courses/{id}/included-in                   -> { id, slug, title }[]   (which courses include this one)
```
`CourseIncludeResponse { includedCourseId, slug, title, status, priceVnd, lessonCount, discountVnd, sortOrder }`.

Errors (400 unless noted): `course.include_self`; `course.include_nested` (the parent is itself included somewhere, or the
child includes others); `course.not_found` (404, either side); `discountVnd` must pass `Vnd.IsValid` and be ≤ the
parent's `priceVnd` (`course.include_discount_too_high`). Adding is idempotent (PUT): an existing pair just updates the
discount. New pairs go to the end of the order.

`CourseAdminDetailResponse` gains `includesCount` and `includedInCount` (cheap counts) so the admin can show them.

## Done when (local DB, clean up after)

Create courses P, Q, C. P includes C (discount 300000) → listed; Q includes C → `included-in` of C lists P and Q.
C includes X → 400 `include_nested`; P includes P → 400; P included by another → 400; discount 1.5 or > price → 400.
Reorder, delete pair. Deleting C while included → 409. Release build `-warnaserror` clean; migration in `make script`.
Update the status table in `../spec.md` and add implementation notes here.

## Implementation notes (done)

Migration `AddCourseIncludes`: new table `courses.course_includes` (PK `course_id, included_course_id`, CHECKs
`ck_course_includes_not_self` / `ck_course_includes_discount_non_negative`, RESTRICT FKs, index
`ix_course_includes_included_course_id`) plus nullable `enrollments.list_price_vnd`, `discount_vnd`, `discount_reason`
(columns exist on the `Enrollment` entity; nothing writes them until B3). Additive only.

Code: `CourseInclude` entity (`Course.Includes` / `Course.IncludedIn`), port `ICourseIncludeRepository` (impl in
Infrastructure), `ICourseIncludeService` / `CourseIncludeService`, `AdminCourseIncludesController`. `CourseService`
now takes `ICourseIncludeRepository` (counts + delete guard).

All routes are `StaffOrAdmin`, base `/api/v1/admins/courses/{id}`:

| Route | Body | Success |
|---|---|---|
| `GET includes` | | 200 `CourseIncludeResponse[]` ordered by `sortOrder` |
| `PUT includes/{includedCourseId}` | `{ "discountVnd": 300000 }` | 200 `CourseIncludeResponse` (add at end, or update discount) |
| `DELETE includes/{includedCourseId}` | | 204 |
| `PUT includes/order` | `{ "includedCourseIds": [..] }` | 204 |
| `GET included-in` | | 200 `{ id, slug, title }[]` ordered by title |

`CourseIncludeResponse`: `{ includedCourseId, slug, title, status ("Draft"|"Published"|"Archived"), priceVnd, lessonCount,
discountVnd, sortOrder }` (`sortOrder` is 0-based). `CourseAdminDetailResponse` gains `includesCount`, `includedInCount`
(populated by GET and PUT of a course; create returns 0/0).

Errors (ProblemDetails with `code`):

| Code | Status | When |
|---|---|---|
| `course.include_self` | 400 | parent == child |
| `course.include_nested` | 400 | parent is itself included somewhere, or child includes others (checked on every PUT) |
| `course.include_discount_too_high` | 400 | `discountVnd` > parent's `priceVnd` |
| `course.not_found` | 404 | parent or child missing (GET/PUT/order/included-in) |
| `course.include_not_found` | 404 | DELETE of a pair that does not exist |
| `order.mismatch` | 400 | reorder list is not exactly the parent's current included ids |
| (validation, `errors.DiscountVnd`) | 400 | not a whole non-negative number (`Vnd.IsValid`) |
| `course.has_includes` | 409 | DELETE of a course that includes others or is included |

Not enforced here: the included course may be a draft (admin should warn); the parent's price is not re-checked when
it is edited later (a stored discount may then exceed it — B3 must clamp at quote time).
