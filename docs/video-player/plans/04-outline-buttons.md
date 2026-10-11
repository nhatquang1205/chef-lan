# Plan 4 — Outline buttons

**Status: done.** `chef-lan-web`, the course page's lesson outline.

| video | button |
|---|---|
| `isFree` and `freeTillSeconds == null` | **XEM VIDEO** / **WATCH VIDEO** |
| `isFree` and `freeTillSeconds != null` | **XEM THỬ** / **WATCH PREVIEW**, with "first N seconds free" next to it |
| not free | a lock icon and "Đăng ký để xem" / "Enroll to watch"; no button |

- The button is a normal link to `/courses/{slug}/watch/{videoId}`, not the Bunny URL. The old external
  "Watch preview" link (new tab, raw embed) goes away.
- If open item 1 is accepted, the outline stops receiving `url`; it only needs `isFree` and `freeTillSeconds`.
- Copy lives in `vi.json` / `en.json`; the existing key `course.watchFree` is split into `watchVideo` and
  `watchPreview`.

## Done when

- all three rows of the table render correctly for a course that has one video of each kind
- the buttons work with the keyboard and have visible focus
