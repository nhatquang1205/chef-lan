# Plan 1 — Playback endpoint

**Status: done.** Backend, `chef-lan` repo.

```
GET /api/v1/videos/{publicId}/playback   anonymous
  -> 200 { videoId, title, url, isFree, freeTillSeconds, expiresAt, courseSlug, courseTitle, lessonTitle }
  -> 403 course.video_not_free      (paid video, no enrollment yet)
  -> 404                            (unknown, deleted, unpublished video, lesson or course)
```

- Loads the video with its lesson and course and requires **all three published** and not deleted, plus a
  published course. A draft course's videos are not reachable by id.
- `url` comes from `IVideoLinkResolver` with `hasFullAccess = false` (enrollments not built yet), so it is
  the same decision as today: free videos only. Never returns the raw `external_id` or stored `url`.
- `expiresAt` = now + `Video:Bunny:TokenLifetimeSeconds`, so the page knows when to re-request.
- When enrollments arrive, `hasFullAccess` comes from the customer's active enrollment and this endpoint
  starts working for paid videos with no change in shape.
- Staff: an admin's token is not recognised on public endpoints yet, so an admin previewing a paid video is a
  later step (see the auth notes in CLAUDE.md).
- Open item 1 in the spec: optionally drop `url` from the course outline in the same change.

## Done when

- a free video returns a URL carrying `token` and `expires`; a paid one is 403; a draft course's video, an
  unpublished video, a deleted lesson and an unknown id are all 404
- the response never contains the video's `externalId`
- with Bunny unset the passthrough resolver still works locally, and still returns nothing for paid videos
