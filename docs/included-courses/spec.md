# Included courses (shared modules) and discounts — spec

Some courses share a whole stage. Example: "Bánh panettone" and "Bánh mì hoa cúc" both start with "Nuôi men tự nhiên"
(natural levain). Instead of copying those lessons, the shared stage is **its own course** (it can also be sold on its
own), and each bigger course **includes** it. A customer who already has the shared stage pays less for the next course.

## Decisions (confirmed by the owner)

| | |
|---|---|
| Model | **Included courses**: course P "includes" course C. One level only (see rules) |
| Access | An active enrollment in P unlocks P's included lessons **and** C's included lessons |
| Discount | **Fixed amount set by staff per include** ("P includes C; if the customer already has C, take 300.000 ₫ off P") |
| "Already has C" | Any active, unexpired path: an enrollment in C, **or** an enrollment in any other course that includes C |
| Where the discount shows | Course page for a signed-in customer; the admin grant dialog (prefilled amount + reason); **recorded on the enrollment** |
| Payment | Unchanged: no checkout. Staff grant by hand; the discount is a suggestion they can override |

## Rules

- **One level:** a course that includes others cannot itself be included, and an included course cannot include others.
  This rules out cycles and keeps the access check to one join.
- A course cannot include itself; the same pair once. Several included courses per course are allowed (ordered).
- The included course must be **published** to appear on the parent's public page (playback already requires the
  lesson's own course to be published). Admin warns when including a draft.
- Only C's **included** lessons (`included_in_course = true`) come with P. C's add-on lessons stay add-ons.
- The discount applies once per included course the customer already has; total discount never takes the price below 0.
- Lesson pages stay under their own course: C's lessons link to `/courses/{C}/lessons/{slug}`.
- Counters: `courses.lesson_count` / `total_duration_seconds` stay **own lessons only** (no cascading recomputes). The
  public course detail returns totals that add the included courses, computed on read.

## Data (additive migrations, nothing breaking)

`courses.course_includes`

| column | type | notes |
|---|---|---|
| `course_id` | bigint FK -> courses | the parent (P) |
| `included_course_id` | bigint FK -> courses | the shared course (C) |
| `discount_vnd` | numeric(12,0) NOT NULL DEFAULT 0 | taken off P when the customer already has C |
| `sort_order` | int NOT NULL | display order on P's page |

PK (`course_id`, `included_course_id`); CHECK `course_id <> included_course_id`; CHECK `discount_vnd >= 0`; FKs RESTRICT.
Index on `included_course_id` (reverse lookups: "who includes C").

`courses.enrollments` gets `list_price_vnd numeric(12,0) NULL`, `discount_vnd numeric(12,0) NULL`,
`discount_reason varchar(300) NULL` — what the course cost and why it was reduced, recorded at grant time.

## Plans

| # | Repo | Plan | Status |
|---|---|---|---|
| B1 | chef-lan | [Schema + admin API for includes](plans/01-schema-and-admin-api.md) | todo |
| B2 | chef-lan | [Access rule + public API](plans/02-access-and-public-api.md) | todo |
| B3 | chef-lan | [Discount quote + recorded on grants](plans/03-discounts-and-grants.md) | todo |
| W  | chef-lan-web | `docs/plans/02-included-courses.md` | todo |
| A  | chef-lan-admin | `docs/plans/15-included-courses.md`, `16-grant-discounts.md` | todo |

Order: B1 → B2 → B3; web after B2 (+B3 for the price line); admin 15 after B1, 16 after B3.
