# Plan 6 — Lesson videos CRUD

**Status: done.** Public `GET /api/v1/courses/{slug}` returns signed URLs for free videos (the open item's assumed answer). A non-free video has no `url` key at all. Recipes and resources are not in the public response. Needs a staff token. See [spec.md](../spec.md) for context.

**Goal:** 6 admin endpoints, lesson counters, and free-video URLs on the public course
detail. This plan closes the loop — after it, a course can actually be published.

---

## Endpoints

```
GET    /api/v1/admins/lessons/{lessonId}/videos       -> LessonVideoAdminResponse[]
POST   /api/v1/admins/lessons/{lessonId}/videos       -> 201 LessonVideoAdminResponse
GET    /api/v1/admins/lesson-videos/{id}              -> LessonVideoAdminResponse
PUT    /api/v1/admins/lesson-videos/{id}              -> LessonVideoAdminResponse
DELETE /api/v1/admins/lesson-videos/{id}              -> 204                  soft delete
PUT    /api/v1/admins/lessons/{lessonId}/videos/order -> 204   { videoIds: [...] }
```

Body: `title`, `description?`, `provider`, `externalId?`, `url?`, `thumbnailUrl?`,
`durationSeconds`, `isFree`, `freeTillSeconds?`, `isPublished`.

**Server-owned:** `sortOrder` (appended on create, changed only via the order endpoint).

`externalId` is **pasted by hand** — there is no upload endpoint in this step, so videos go
up through the Bunny dashboard and the GUID is copied across. Upload tickets are a later
step.

## Resolver — already built

`IVideoLinkResolver` was changed in [plan 1](01-schema-and-domain.md) out of necessity, so
this plan only consumes it:

```csharp
ValueTask<string?> ResolveAsync(
    LessonVideo video, bool hasFullAccess, long? customerId, CancellationToken ct = default);
```

| `hasFullAccess` | `is_free` | returns |
|---|---|---|
| `true` | — | signed URL |
| `false` | `true` | signed URL (+ the frontend honours `freeTillSeconds`) |
| `false` | `false` | `null` |

`hasFullAccess` is always `false` on customer routes in this step — enrollment gating arrives
with the enrollment endpoints. So the public course detail exposes free videos and nothing
else.

## Counters

`lessons.video_count` and `lessons.duration_seconds` are recomputed inside the same
transaction as any create / update / delete, counting published non-deleted videos only, and
`courses.total_duration_seconds` is rolled up from the lessons.

## Files

- `ChefLan.Logic/Courses/Interfaces/` — `ILessonVideoRepository`, `ILessonVideoService`
- `ChefLan.Logic/Courses/Contracts/`, `Validators/`, `Mapping/LessonVideoMappings.cs`
- `ChefLan.Infrastructure/Repositories/Courses/LessonVideoRepository.cs`
- `ChefLan.Courses.API/Controllers/Admin/AdminLessonVideosController.cs`
- extend `CourseDetailResponse` with per-video `url` (free only), `isFree`,
  `freeTillSeconds`, `durationSeconds`

## Done when

- 2 videos on a lesson → `lessons.video_count = 2`, `duration_seconds` summed, and
  `courses.total_duration_seconds` rolled up
- unpublishing one video → `video_count` drops to 1, durations recomputed
- reorder reversed → rewritten in one transaction, no index violation
- reorder with a duplicate / missing / foreign id → 400, nothing changed
- `freeTillSeconds` without `isFree` → **400** (the DB CHECK would also reject it; the 400
  is so the admin UI gets a usable message instead of a 500)
- `freeTillSeconds = 0` → 400
- `isPublished = true` with neither `externalId` nor `url` → 400
- `isPublished = false` with neither → **accepted**, so a draft video row is saveable
- `provider = Bunny` with no `externalId` and `isPublished = true` → 400

**Access matrix on `GET /api/v1/courses/{slug}`** — the security boundary of the product:

| video state | expected |
|---|---|
| `is_free = true`, `free_till = NULL` | signed URL, `freeTillSeconds: null` |
| `is_free = true`, `free_till = 120` | signed URL, `freeTillSeconds: 120` |
| `is_free = false` | metadata only, **no URL field populated** |
| `is_published = false` | absent from the response entirely |

- with `Video:Bunny` unset, free URLs come from `PassthroughVideoLinkResolver` (local only);
  non-free videos still resolve to `null`, because entitlement is checked before provider
- with `Video:Bunny` set, free URLs are signed embeds with `token` and `expires`

**And the publish flip:**

- `POST /api/v1/admins/courses/{id}/publish` → **204** now that a published lesson has a
  playable video. This is the case [plan 4](04-courses-crud.md) and
  [plan 5](05-lessons-crud.md) both expect to 409.
- unpublish every video, then publish the course → back to 409

Finally, `DELETE` → soft, counters decremented, and the video gone from both the admin list
and the public course detail.
