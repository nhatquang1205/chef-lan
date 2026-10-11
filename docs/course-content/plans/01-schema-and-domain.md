# Plan 1 — Schema and domain

**Status: done.** Migration `20261007133505_AddCategoryStatusAndLessonVideos` applied.
See [spec.md](../spec.md) for context and shared rules.

**Goal:** the tables exist and the model compiles. No endpoints.

---

## `courses.categories` — add status

| column | type | notes |
|---|---|---|
| `status` | `integer NOT NULL DEFAULT 1` | `CategoryStatus`: `Active=1`, `InActive=2` |

- `CHECK (status IN (1, 2))`, comment `"1=Active, 2=InActive"`
- `ix_categories_status` (`status`) WHERE `is_deleted = false`
- **`uq_categories_slug` unchanged** — still filtered on `is_deleted = false` only, not on
  status. An InActive category keeps owning its slug, otherwise reactivating it could
  collide with a newer one.

## `courses.lessons` — drop video columns, add recipes

| column | change |
|---|---|
| `recipes` | **added** `text NULL` — markdown |
| `video_count` | **added** `integer NOT NULL DEFAULT 0` |
| `duration_seconds` | **kept**, now derived: sum of published videos |
| `video_provider`, `video_url`, `video_external_id`, `is_free_preview`, `thumbnail_url` | **dropped** |

`ck_lessons_published_has_video` and `ck_lessons_video_provider` dropped — they guarded the
dropped columns. The equivalent rule is now `ck_lesson_videos_published_has_source`.
`ck_lessons_video_count_non_negative` added. `ix_lessons_course_id_sort_order` stays
non-unique.

## `courses.lesson_videos` — new

| column | type | notes |
|---|---|---|
| `id` | `bigint` identity | PK |
| `lesson_id` | `bigint NOT NULL` | FK → `courses.lessons(id)` ON DELETE CASCADE |
| `sort_order` | `integer NOT NULL` | |
| `title` | `varchar(200) NOT NULL` | "Phần 1 — Nhào bột" |
| `description` | `text NULL` | |
| `provider` | `integer NOT NULL DEFAULT 5` | `VideoProvider`; `Bunny=5` |
| `external_id` | `varchar(100) NULL` | Bunny video GUID |
| `url` | `varchar(1000) NULL` | providers addressed by URL (YouTube, legacy Facebook) |
| `thumbnail_url` | `varchar(1000) NULL` | |
| `duration_seconds` | `integer NOT NULL DEFAULT 0` | |
| `is_free` | `boolean NOT NULL DEFAULT false` | |
| `free_till_seconds` | `integer NULL` | NULL = whole video free |
| `is_published` | `boolean NOT NULL DEFAULT true` | |
| *audit + soft-delete + xmin* | | |

- `ix_lesson_videos_lesson_id_sort_order` WHERE `is_deleted = false` — **not unique**
- CHECKs: `ck_lesson_videos_sort_order_non_negative`,
  `ck_lesson_videos_duration_non_negative`, `ck_lesson_videos_provider`,
  `ck_lesson_videos_free_till_positive`, `ck_lesson_videos_free_till_requires_free`,
  `ck_lesson_videos_published_has_source`

---

## Deviation from the original plan

`IVideoLinkResolver` was scheduled for plan 6, but dropping `Lesson.VideoProvider`,
`VideoUrl` and `VideoExternalId` breaks both resolvers — **this plan cannot compile without
the change**, so it moved here:

```csharp
ValueTask<string?> ResolveAsync(
    LessonVideo video, bool hasFullAccess, long? customerId, CancellationToken ct = default);
```

Both implementations now check **entitlement before provider**, so a non-free video returns
`null` regardless of where it is hosted — including `PassthroughVideoLinkResolver`, which
previously handed back any stored URL unconditionally. Plan 6 consumes this rather than
changing it.

## Files

- `ChefLan.Domain/Courses/Entities/LessonVideo.cs` — new; `Lesson.cs` reshaped
- `ChefLan.Domain/Enums/CategoryStatus.cs` — new; `Category.Status` added
- `ChefLan.DB/Configurations/Courses/LessonVideoConfiguration.cs` — new;
  `LessonConfiguration` and `CategoryConfiguration` updated
- `ChefLan.DB/Contexts/CoursesDbContext.cs` — `DbSet<LessonVideo>`
- `ChefLan.Logic/Courses/Video/IVideoLinkResolver.cs` — new signature
- `ChefLan.Infrastructure/Services/Video/{Bunny,Passthrough}VideoLinkResolver.cs` — updated
- `ChefLan.DB/Migrations/Courses/20261007133505_AddCategoryStatusAndLessonVideos.cs`

---

## Done when — all verified

```bash
make migrate && make build && make run
docker exec cheflan-postgres psql -U cheflan -d cheflan -c '\d+ courses.lesson_videos'
docker exec cheflan-postgres psql -U cheflan -d cheflan -c '\d+ courses.lessons'
docker exec cheflan-postgres psql -U cheflan -d cheflan -c '\d+ courses.categories'
```

| check | result |
|---|---|
| `lessons` free of `video_*` / `is_free_preview`; has `recipes`, `video_count` | pass |
| `lesson_videos` exists, all 6 CHECKs present | pass |
| `external_id`, `url`, `free_till_seconds` nullable; the rest NOT NULL | pass |
| `ix_lesson_videos_lesson_id_sort_order` **not** unique | pass (`uniq = f`) |
| `categories.status` CHECK + comment | pass |
| `uq_categories_slug` still filtered on `is_deleted` only | pass |
| full rebuild, 0 warnings | pass |
| `GET /api/v1/courses` 200, `/health` Healthy | pass |

Constraint behaviour, exercised rather than assumed:

| case | expected | result |
|---|---|---|
| two videos sharing a `sort_order` | accepted | pass |
| `free_till_seconds` with `is_free = false` | rejected | pass |
| `free_till_seconds = 0` | rejected | pass |
| `free_till_seconds = 120` with `is_free = true` | accepted | pass |
| `is_free = true`, `free_till NULL` | accepted | pass |
| published with neither `external_id` nor `url` | rejected | pass |
| **un**published with no source (draft row) | accepted | pass |
| negative `duration_seconds` | rejected | pass |
| `provider = 9` | rejected | pass |
| `categories.status = 3` | rejected | pass |
| `categories.status = 2` | accepted | pass |

Column drops were safe — `lessons` had 0 rows. The same change after launch would need a
data move.
