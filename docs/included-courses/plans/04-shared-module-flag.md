# Plan 04: mark a course as a shared module

Why: today any course can be picked as an include, and shared modules sit in the public catalog next to the full courses.
Staff now mark them explicitly. The picker shows only those, and the public lists leave them out.

## Decisions

| | |
|---|---|
| Flag | `courses.is_shared_module boolean NOT NULL DEFAULT false` |
| Existing data | The migration sets it on every course already in `course_includes.included_course_id`, or those rows break the new rule |
| Public list | `GET /api/v1/courses` leaves them out. That covers the /courses page, search, category pages, hero and home category sections |
| New endpoint | `GET /api/v1/shared-modules`, `[AllowAnonymous]`, paged, same parameters and shape as `GET /api/v1/courses`. A separate path, so it can never collide with a course slug |
| Unchanged | `GET /courses/{slug}`, the lesson page, `/access`, `/me/courses`, orders: a shared module stays reachable and can still be bought alone |
| Admin list | Still shows everything, with a new `isSharedModule` filter |
| Only flagged courses can be included | `UpsertAsync` refuses another course: 409 `course.include_not_shared` |
| Unflagging one that is in use | Refused: 409 `course.shared_in_use` |
| One level | A flagged course cannot include others, and a course with includes cannot be flagged: 409 `course.shared_cannot_include` |

## Build

1. Domain `Course.IsSharedModule`; `CourseConfiguration` (default false, comment), partial index on published shared modules if the
   list query needs one. `make migration NAME=AddCourseIsSharedModule`; add the backfill with `migrationBuilder.Sql`. `make migrate`.
2. `CourseQueryExtensions`: `OnlyRegularCourses()`, `OnlySharedModules()`, `WithSharedModule(bool?)`.
3. `CourseRepository`: `ListPublishedAsync` excludes shared modules, a new `ListPublishedSharedModulesAsync`, and `ListAsync` applies
   `AdminCourseListQuery.IsSharedModule`. `CourseCatalogService.ListSharedModulesAsync` maps exactly like `ListPublishedAsync`.
4. `CoursesController` is routed by `[controller]`: add `SharedModulesController` at `api/v1/shared-modules`.
5. `CourseRequest` and `CourseFormRequest` (multipart) gain `bool IsSharedModule = false`. A PUT replaces the whole course, so it
   sets the flag. `CourseService` create and update enforce the rules above, in one place. Errors in `CourseErrors`.
6. `IsSharedModule` on `CourseListItemResponse` and the admin detail response. `CourseIncludeService.UpsertAsync` enforces both rules.
7. Docs: `../spec.md` (decisions, rules, plan table), the `CLAUDE.md` section on lesson prices and access if it needs a line.

## Done when

`dotnet build ChefLan.slnx -c Release -warnaserror` and `dotnet test tests/ChefLan.Logic.Tests` pass, with tests for the rules if the
fakes allow it. Against the local API: a flagged published course is gone from `GET /courses` (also with `q` and `categorySlug`), is in
`GET /shared-modules`, and its `GET /courses/{slug}` is still 200. A customer owning it still sees it in `/me/courses`. Each 409 above
fires, and a multipart PUT keeps the flag. The backfill sets the flag on a course that was already included. Test data removed.

## Implementation notes

- Done. Build with `-warnaserror` clean; `ChefLan.Logic.Tests` 61 passed (no new tests: the fakes cover payments only, there is no course fake).
- No index added: the courses table is small and the existing `(status, published_at)` index serves the list.
- Rule checks: `CourseIncludeService.UpsertAsync` checks parent-is-shared, then child-not-shared, before the nested check. `CourseService.UpdateAsync` checks only when the flag changes.
- Create cannot conflict (a new course has no includes), so only update enforces `shared_cannot_include` and `shared_in_use`.
- `/me/courses` was not exercised with a real customer (no password-bearing local customer; creating one would risk mail); its query does not use the new filters.
- Verified locally: backfill flagged an already-included course; `/courses` (plain, `q`, `categorySlug`) hides a flagged published course, `/shared-modules` lists it (also with `categorySlug`), `/courses/{slug}` stays 200; all three 409s; multipart PUT and POST keep/set the flag.
