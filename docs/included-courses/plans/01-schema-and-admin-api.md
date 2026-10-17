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
