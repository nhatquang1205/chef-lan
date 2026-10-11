# Course content — spec

Admin CRUD for the catalog Chef Lan actually fills in: categories, courses, lessons, and the
videos inside a lesson. Plus the customer-facing reads that expose it.

This folder is the source of truth. The approved plan at
`~/.claude/plans/i-want-to-build-zany-elephant.md` is a snapshot of the same content and will
go stale.

## Plans

| # | Plan | Endpoints | Needs staff token | Status |
|---|---|---|---|---|
| 1 | [Schema and domain](plans/01-schema-and-domain.md) | — | no | **done** |
| 2 | [`ICurrentUser` middleware](plans/02-current-user.md) | — | no | **moved to `docs/auth/`** |
| 3 | [Categories CRUD](plans/03-categories-crud.md) | 5 admin + 1 customer | yes | **done** |
| 4 | [Courses CRUD](plans/04-courses-crud.md) | 7 admin + 1 customer | yes | **done** |
| 5 | [Lessons CRUD](plans/05-lessons-crud.md) | 6 admin | yes | **done** |
| 6 | [Lesson videos CRUD](plans/06-lesson-videos-crud.md) | 6 admin | yes | **done** |

**24 admin, 3 customer** endpoints when complete.

**Unblocked, and done.** Plans 3–6 needed a staff token, which
[`docs/auth/plans/03-staff-auth.md`](../auth/plans/03-staff-auth.md) provides. Plan 2 moved
into that feature as `01-current-user.md`.

---

## Context

The backend has a schema and one read-only catalog endpoint. Chef Lan cannot put anything
into it yet: no way to create a category, a course, a lesson, or attach a video.

Today she sells by creating one Facebook group per course, posting cooking videos into it,
and adding each paying customer by hand. There is no system of record — courses, prices, who
paid and who still has access live in her head and in Facebook.

Three business requirements drive schema change beyond plain CRUD:

1. **Categories need Active/InActive**, so a seasonal category ("Bánh Trung Thu") can be
   hidden in March and shown in August without deleting it and breaking course links.
2. **A lesson may hold more than one video** — "Phần 1 — Nhào bột", "Phần 2 — Nướng".
   Today `lessons` carries one video inline.
3. **A video may be free, or free only up to a given second.**

## Decisions

| | |
|---|---|
| Admin routes | `/api/v1/admins/...` (**plural**) |
| Customer routes | `/api/v1/...`, **read-only** — no POST/PUT/DELETE outside `/admins/` |
| Course endpoints | do **not** create lessons |
| `categories.status` | `Active`/`InActive` gates the storefront; `is_deleted` stays for real removal |
| `lessons.recipes` | markdown `text` column |
| `lesson_videos` | own table, many videos per lesson; video columns **moved off** `lessons` |
| `free_till_seconds` | simple field, **enforced by the frontend**. No preview clip, no pipeline |
| Authorization | middleware → one `ICurrentUser` with `Source` + guarded id accessors |
| Endpoints | CRUD + reorder + course publish/unpublish |

### Why markdown for `recipes`

Markdown → HTML is trivial; HTML → markdown is lossy, so markdown keeps the option open and
HTML closes it. Editor-produced HTML is also a stored-XSS surface, and with a `Staff` role
coming the author will not always be the owner. The usability concern is a **UI** problem:
the admin editor should be a toolbar that *emits* markdown (TipTap/Milkdown), so she clicks
**B** and we store `**bold**`. Render with raw HTML disabled.

### Out of scope

Upload tickets / Bunny TUS, preview encoding, enrollment and playback gating, pricing
endpoints, category reorder endpoint, archive action, Staff-vs-Admin role split.

---

## Gate before plans 3–6 — resolved

Every admin endpoint is `[Authorize]`, and staff login did not exist. **Decision: build auth
first**, as its own feature — see [`docs/auth/spec.md`](../auth/spec.md). Plans 3–6 become
testable once [staff auth](../auth/plans/03-staff-auth.md) lands.

## Resolved

**Does public `GET /api/v1/courses/{slug}` return a playable URL for `is_free` videos?**
Plan 6 assumes **yes**. Returning them is the natural meaning of `is_free` and needs no
enrollment logic. Decide before plan 6.

---

## Shared reference

### `free_till_seconds` semantics

| `is_free` | `free_till_seconds` | non-enrolled viewer gets |
|---|---|---|
| `false` | anything | nothing — 403 |
| `true` | `NULL` | the whole video |
| `true` | `120` | first 120s, **stopped by the frontend** |

The API returns the signed URL plus `freeTillSeconds`. Flagged once: this is bypassable by
seeking or reading the URL out of the DOM, so it is a teaser limit, **not a paywall**.
Anything genuinely paid must have `is_free = false`.

### Reorder endpoints

`(parent_id, sort_order)` is deliberately **non-unique** — a partial unique index cannot be
`DEFERRABLE` in Postgres, so a unique one rejects the intermediate states of a drag-and-drop
reorder. Both reorder endpoints take the full ordered id list and rewrite every row of the
parent **in one transaction**. Reject `400` unless the list is exactly the parent's current
live children.

### Denormalized counters

`courses.lesson_count`, `courses.total_duration_seconds`, `lessons.video_count` and
`lessons.duration_seconds` are maintained by the services **inside the same transaction** as
the write that changes them, counting published non-deleted rows only. `xmin` concurrency
tokens on `Course` and `Lesson` catch concurrent edits.

### Patterns to reuse, not reinvent

`Result<T>` / `Error` + `ResultToActionResultExtensions`, `PagedRequest` / `PagedResult<T>`,
`ISlugGenerator.GenerateUnique`, `IClock`, `Vnd.IsValid`, `IUnitOfWork`, the auditing and
soft-delete interceptors, `CourseQueryExtensions`.

Layering rules are in `CLAUDE.md` — in particular, EF Core stays in `ChefLan.DB` and
`ChefLan.Infrastructure`, ports never expose `IQueryable`, and services are
`internal sealed` behind one public DI entry point per domain folder.

---

## Verification across the whole step

After plan 6, one pass end to end with a staff token:

1. create category → create course → publish → **409** (no lessons)
2. create 3 lessons → add 2 videos to one → publish → 204
3. `GET /api/v1/courses` shows it; `GET /api/v1/courses/{slug}` shows the outline with free
   video URLs only
4. reorder lessons and videos; counters stay correct
5. set the category InActive → drops off `GET /api/v1/categories`, course link intact
6. **customer token on every `/admins/*` route → 401**, and `ICurrentUser` reports
   `Source = Customer` with `TryGetStaffId` false

Step 6 deserves an integration test under `tests/` with `Testcontainers.PostgreSql`. It is
the security boundary, and `ICurrentUser` replacing the two separate accessors in plan 2 is
exactly the change that could weaken it.
