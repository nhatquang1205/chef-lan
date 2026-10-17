# B2 — Access rule and public API

Needs B1. Read `../spec.md`.

## Access (one place)

`IEnrollmentRepository.HasAccessAsync(customerId, courseId, lessonId, utcNow)` today: course enrollment in the lesson's
course (lesson included) OR lesson enrollment. **Add:** OR an active, unexpired enrollment in **any course that includes
the lesson's course** (and the lesson is `included_in_course`). One query with a join on `course_includes`.

Add `IEnrollmentRepository.OwnedCourseIdsAsync(customerId, IEnumerable<long> courseIds, utcNow)` → the subset of the given
courses the customer "has": a direct active enrollment, or an active enrollment in any course that includes it. Used by
the access endpoint and B3.

Staff viewers keep full access (unchanged). Playback and the lesson page need no other change: they call HasAccessAsync.

## Public course detail (`GET /api/v1/courses/{slug}`)

`CourseDetailResponse` gains:
```
includedCourses: [{ slug, title, subtitle?, thumbnailUrl?, lessonCount, totalDurationSeconds,
                    lessons: LessonOutlineResponse[] }]      // published included courses only, in sort order;
                                                              // only their included lessons (no add-ons)
totalLessonCount, totalDurationSecondsWithIncluded            // own + included, computed on read
```
Discount amounts are **not** in the public response (they only matter to a signed-in customer; see B3).

Also `includedIn: [{ slug, title }]` (published parents) so the shared course's own page can say "Có trong khóa: …".

## Access endpoint (`GET /api/v1/courses/{slug}/access`, AnyUser)

Add `includedCourses: [{ slug, hasCourseAccess, lessonIds }]` for each included course: `hasCourseAccess` true when the
customer has P (P's enrollment unlocks C's included lessons) or has C by any path; `lessonIds` = their lesson grants in C.
Staff: everything true.

## Done when (local DB, clean up after)

Customer enrolled in P only: playback of a paid video in C's included lesson → 200; C's add-on lesson → 403; the lesson
page of C's lesson → `hasAccess: true`, recipe returned. Revoke P → 403 again. Enrolled in C only: P's own paid video → 403.
Course detail of P shows `includedCourses` with C's lessons and the totals; C shows `includedIn: [P]`. Draft C is omitted
from P's page. Release build clean. Update status + notes.

## Implementation notes (done)

No migration. `IEnrollmentRepository.HasAccessAsync` now also accepts an active, unexpired enrollment in any course that
includes the lesson's course (lesson must be `included_in_course`; one `EXISTS` on `course_includes`). New
`IEnrollmentRepository.OwnedCourseIdsAsync(customerId, courseIds, utcNow)` -> `IReadOnlySet<long>` (direct enrollment or via a
parent), ready for B3. New `ICourseIncludeRepository.ListPublishedIncludedWithLessonsAsync` / `ListPublishedParentsAsync`
(read-only; draft/archived courses omitted). `EnrollmentService` and `CourseCatalogService` take `ICourseIncludeRepository`.

`GET /api/v1/courses/{slug}` (`CourseDetailResponse`) gains, besides the existing fields:

```
includedCourses: [{ slug, title, subtitle?, thumbnailUrl?, lessonCount, totalDurationSeconds,
                    lessons: LessonOutlineResponse[] }]   // published included courses, sort order; only published
                                                          // lessons with includedInCourse = true; no discount amounts
includedIn: [{ slug, title }]                             // published parents, by title
totalLessonCount                                          // own lessonCount + sum(included lessonCount)
totalDurationSecondsWithIncluded                          // own + included durations
```
Arrays are `[]` (never absent) when empty. `lessonCount` / `totalDurationSeconds` of each included course are its stored
counters (own included lessons only).

`GET /api/v1/courses/{slug}/access` (`CourseAccessResponse`) gains:
```
includedCourses: [{ slug, hasCourseAccess, lessonIds }]   // one per published included course, parent's order
```
`hasCourseAccess` = customer has the parent, or has that course by any path (direct or another parent). `lessonIds` = that
course's lesson-level grants (active, unexpired). Staff: all `true`, `lessonIds` = every published included lesson.

Playback and the lesson page needed no change (they call `HasAccessAsync`); a lesson page stays under its own course
(`/courses/{C}/lessons/{slug}`).

Verified locally: P-only customer -> C included video 200, C add-on 403, recipe returned; after revoking P -> 403 and recipe
locked; C-only customer -> P's paid video 403; draft C omitted from P's page; C shows `includedIn: [P]`.
