# Plan 5 — Lessons CRUD

**Status: done.** Counters are recomputed in memory from the tracked entities and saved in the same `SaveChanges`, so no explicit transaction is needed. Deleting a lesson leaves gaps in `sort_order` (harmless: the index is non-unique and ordering still holds) and does not cascade to its videos. Needs a staff token. See [spec.md](../spec.md) for context.

**Goal:** 6 admin endpoints, plus the reorder pattern and the course counters.

---

## Endpoints

```
GET    /api/v1/admins/courses/{courseId}/lessons       -> LessonAdminListItemResponse[]
POST   /api/v1/admins/courses/{courseId}/lessons       -> 201 LessonAdminDetailResponse
GET    /api/v1/admins/lessons/{id}                     -> LessonAdminDetailResponse
PUT    /api/v1/admins/lessons/{id}                     -> LessonAdminDetailResponse
DELETE /api/v1/admins/lessons/{id}                     -> 204                  soft delete
PUT    /api/v1/admins/courses/{courseId}/lessons/order -> 204   { lessonIds: [3,1,2] }
```

Body: `title`, `description?`, `recipes?` (markdown), `resourcesUrl?`, `isPublished`.

**Server-owned:** `slug` (generated, unique **within the course**), `sortOrder` (appended on
create, changed only via the order endpoint), `durationSeconds`, `videoCount`.

Create is nested under the course; item operations are flat, so the frontend does not need
to carry the course id around once it has a lesson id.

## Reorder

`ix_lessons_course_id_sort_order` is **not unique** on purpose — a partial unique index
cannot be `DEFERRABLE` in Postgres, so a unique one would reject the intermediate states of a
drag-and-drop reorder. Uniqueness is this endpoint's job:

- take the full ordered list, rewrite every row of the course in **one transaction**
- reject `400` unless the list is exactly the course's current live lesson ids — no
  duplicates, nothing missing, nothing belonging to another course
- a course has well under 50 lessons, so a full rewrite is trivial and the invariant lives in
  one testable method

## Counters

`courses.lesson_count` and `courses.total_duration_seconds` are recomputed inside the same
transaction as any create / update / delete, counting published non-deleted rows only.
`lessons.duration_seconds` and `videoCount` stay 0 until plan 6 adds videos.

## Files

- `ChefLan.Logic/Courses/Interfaces/ILessonRepository.cs` — extend
- `ChefLan.Logic/Courses/Interfaces/ILessonService.cs`
- `ChefLan.Logic/Courses/Contracts/`, `Validators/`, `Mapping/LessonMappings.cs`
- `ChefLan.Infrastructure/Repositories/Courses/LessonRepository.cs` — implement the stub
  (currently throws `NotImplementedException`)
- `ChefLan.Courses.API/Controllers/Admin/AdminLessonsController.cs`

## Done when

- 3 lessons created → `sortOrder` 0, 1, 2 appended; `courses.lesson_count = 3`
- two lessons in **different** courses may share a slug
- two lessons in the **same** course with the same title → second gets `-2`, no 409 from the
  unique index
- reorder reversed → rows rewritten, **no unique-index violation**, single transaction
- reorder with a duplicate id → 400, nothing changed
- reorder omitting an id → 400, nothing changed
- reorder including a lesson from another course → 400, nothing changed
- `recipes` round-trips markdown byte-for-byte, including Vietnamese and newlines
  (`## Nguyên liệu\n- 500g bột mì số 13`)
- `POST` to a soft-deleted or unknown `courseId` → 404
- `DELETE` → soft; `lesson_count` decremented; the lesson's slug reusable in that course
- **`POST /api/v1/admins/courses/{id}/publish` still returns 409.** The rule is "a published
  lesson *with a playable video*", and videos arrive in plan 6 — so lessons alone are not
  enough. The 409 message must say which of the two is missing, or this is impossible to
  debug from the frontend.
- customer token on any `/admins/*` route → 401

> The approved plan claimed publish flips to 204 here. It does not — the publish rule needs a
> video, so the flip lands in [plan 6](06-lesson-videos-crud.md).
