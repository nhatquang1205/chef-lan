# Lesson prices and lesson access — spec

A lesson can have its own price. Owning a **course** includes every lesson; a customer may instead buy a
**single lesson**. Staff grant access by hand for now (there is no checkout or payment gateway).

## Decisions

| | |
|---|---|
| Lesson price | `lessons.price_vnd`, optional. **NULL = not sold separately** (only through the course). Whole dong, >= 0 |
| Lesson access | new table `courses.lesson_enrollments`, one row per customer and lesson, the twin of `enrollments` |
| Course access | existing `courses.enrollments`; it unlocks **every lesson**, including ones added later |
| Lesson access period | **the lesson's course's rule**: lifetime if the course has no `access_duration_days`, else that many days from the grant (snapshotted at grant time) |
| Owning lessons, then the course | nothing special: full course price, the lesson grants simply stay. Any credit is a manual staff decision |
| Granting | staff only: `POST /admins/enrollments` (course), `POST /admins/lesson-enrollments` (lesson), by `customerId` **or** `customerEmail` |
| Payment | recorded on the grant (`isPaid`, `amountPaidVnd`, note, reference); no gateway, no checkout |

## Access rule

A customer may watch a **non-free** video when they have an **active, unexpired course enrollment for its
course OR an active, unexpired lesson enrollment for its lesson** (video, lesson and course all published).
Owners get no `freeTillSeconds` limit (`hasFullAccess: true`). Free videos stay free for everyone, with their
limit for non-owners. Revoked, expired and pending rows give no access.

## Endpoints

```
PUT    /api/v1/admins/lessons/{id}                     lessons gain "priceVnd" (null clears it)
POST   /api/v1/admins/enrollments                      grant a COURSE   {courseId, customerId|customerEmail, isPaid?, amountPaidVnd?, paymentNote?, paymentReference?, source?}
POST   /api/v1/admins/lesson-enrollments               grant a LESSON   {lessonId, ...same}
POST   /api/v1/admins/enrollments/{id}/revoke          204
POST   /api/v1/admins/lesson-enrollments/{id}/revoke   204
GET    /api/v1/admins/customers/{customerId}/access    everything a customer holds, any status
GET    /api/v1/courses/{slug}/access                   customer token: {hasCourseAccess, lessonIds} for the course page
GET    /api/v1/videos/{publicId}/playback              now unlocks for owners; response gains hasFullAccess
```

The public course outline's lessons carry `priceVnd` / `priceDisplay`.

Granting an access that is still active is a **409** (so a double click cannot double-count a payment); a
revoked or expired row is **reactivated** and its period restarted. Staff cannot grant with `source:
OnlineCheckout` (reserved for the future checkout). `courses.enrollment_count` is kept in step in the same
transaction.

## Why the web asks for playback from the browser

Login tokens live in the browser, so a server-rendered page cannot know who is watching. The watch page and
the course page's "what do I own" overlay therefore call the API from the browser with the customer's token.
The API is what enforces access; the overlay only changes which buttons show.

## Not built

- online checkout / payment gateway, orders, webhooks
- a customer-facing "my courses" list
- an admin UI (grants are curl/Postman)
- staff tokens on public endpoints: an admin signed in on the web still sees paid videos as locked
- crediting lesson purchases toward a later course purchase

## Add-on lessons (`lessons.included_in_course`)

A lesson is either part of the course price (`included_in_course = true`, the default) or an **add-on**
(`false`): sold only on its own. Rules:

- A course enrollment unlocks only included lessons. An add-on needs an active `lesson_enrollments` row
  (`IEnrollmentRepository.HasAccessAsync` checks the flag on the course branch).
- An add-on must have a price (`ck_lessons_addon_has_price`, plus a validator 400). An included lesson may
  still carry a price and be sold alone, as before.
- `courses.lesson_count` and `total_duration_seconds` count included, published lessons only. Publishing a
  course requires at least one published included lesson.
- Access period for an add-on grant is the course's `access_duration_days`, as for any lesson grant.
- Flipping an existing lesson to add-on removes it from people who bought the course; flipping it back gives
  it to every course buyer. The PUT does this without a warning.
- The public outline lists add-ons with `includedInCourse: false`; the course page shows them in their own group.
