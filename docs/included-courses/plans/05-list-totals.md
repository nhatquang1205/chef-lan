# Plan 05: course list totals include shared modules

Why: the course page already counts the published shared modules a course includes (`totalLessonCount`,
`totalDurationSecondsWithIncluded`), but the course cards (home page, `/courses`) show only the course's own lessons, so a
course looks shorter on its card than on its page.

## Decisions (confirmed)

| | |
|---|---|
| Fields | `CourseListItemResponse` gains `TotalLessonCount` and `TotalDurationSecondsWithIncluded`, same meaning as on the course page: own published included lessons + every **published** included course's `LessonCount` / `TotalDurationSeconds` |
| Old fields | `LessonCount` / `TotalDurationSeconds` unchanged (own lessons only) |
| Where | Public `GET /api/v1/courses` computes them. Everywhere else the mapping sets them equal to the own values (shared modules include nothing; the admin list is unchanged) |
| Web | Course cards show the totals |

## Build (backend, `chef-lan`)

1. `CourseListItemResponse`: `public int TotalLessonCount { get; init; }` and `public int TotalDurationSecondsWithIncluded { get; init; }`
   with a one-line doc each. `CourseMappings.ToListItemResponse` sets them to `LessonCount` / `TotalDurationSeconds`.
2. `ICourseIncludeRepository.SumPublishedIncludedAsync(IReadOnlyCollection<long> courseIds)`:
   `IReadOnlyDictionary<long, (int LessonCount, int DurationSeconds)>`, one grouped query over `course_includes` joined to the
   included course, only included courses that are published (and not deleted, by the query filter), keyed by parent id.
   Empty input returns an empty dictionary without a query.
3. `CourseCatalogService` public list: after loading the page, call (2) once with the page's ids and add the sums to the totals
   (`with { ... }`). `GetPublishedBySlugAsync` keeps its own computation (it already loads the included courses).
4. Docs: status row in `../spec.md`; implementation notes here.

## Build (web, `chef-lan-web`)

5. `src/lib/api/types.ts`: `CourseListItem` gains `totalLessonCount?: number` and `totalDurationSecondsWithIncluded?: number`
   (optional: an older API omits them).
6. `CourseCard`: show `course.totalLessonCount ?? course.lessonCount` and
   `course.totalDurationSecondsWithIncluded ?? course.totalDurationSeconds`. Nothing else changes.
7. `tests/mock-api.mjs`: give one listed course totals larger than its own counts; an e2e assertion that its card shows them.
   Note it in `docs/plans/06-personalised-home.md` implementation notes (one line) or a new short plan file `07-list-totals.md`.

## Done when

Backend build `-warnaserror` and `dotnet test tests/ChefLan.Logic.Tests` clean. Web `pnpm typecheck`, `pnpm eslint src tests`,
`pnpm build` (API stopped), `pnpm test:e2e` clean. Local API (local DB, data removed afterwards): a course including a
published module of 3 lessons / 1 h lists `totalLessonCount = own + 3` and duration `own + 3600`; an unpublished module adds
nothing; `/api/v1/shared-modules` totals equal the own values.

## Implementation notes

- `CourseListItemResponse.TotalLessonCount` / `TotalDurationSecondsWithIncluded` are `required`; `ToListItemResponse` sets them to the own values, so the admin list, shared-module list and course detail summary are unchanged. Only `CourseCatalogService.ListPublishedAsync` adds the included sums (`WithIncluded`, one `SumPublishedIncludedAsync` call per page; parents that include nothing are absent from the result).
- `SumPublishedIncludedAsync` is one grouped query over `course_includes` filtered to published included courses (soft-deleted ones drop out through the query filter), summing the denormalized `lesson_count` / `total_duration_seconds`. Fakes needed no change (`Stub` is a dispatch proxy).
- Web: `CourseCard` falls back to the own counts when an older API omits the totals; the mock gives Macaron 5 lessons / 4800 s and `home.spec.ts` asserts its card. Noted in the web repo's `docs/plans/06-personalised-home.md`.
- Local API check (local DB, restored afterwards): a course with a published module (2 lessons, 3030 s) and a draft one listed `totalLessonCount` 4 and 5765 s (own 2 and 2735 s), the same as its course page; the draft module added nothing; shared-module totals equal their own values.
