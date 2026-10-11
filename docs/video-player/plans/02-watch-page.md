# Plan 2 — Watch page

**Status: done.** `chef-lan-web`. Needs plan 1.

`/courses/{slug}/watch/{videoId}` (and `/en/...`), in the normal site layout.

- Server component fetches `GET /videos/{id}/playback` (uncached, inside Suspense, like the course page).
  404 from the API -> the not-found page; 403 -> a "this video is not free" message with a way back to the
  course; API down -> the usual error state.
- Layout: title, "course > lesson" breadcrumb back to the course, the player in a 16:9 box, and under it the
  video description. A side or bottom list of the lesson's other videos is a later nicety.
- The player is a client component holding the `<iframe src={url}>`; it loads Bunny's player.js script from
  Bunny's CDN. Allow the iframe only what it needs (`allow="autoplay; encrypted-media; picture-in-picture"`,
  `allowfullscreen`), and `referrerpolicy` as the Bunny security setting requires.
- The signed link expires. If the page is left open past `expiresAt` and the viewer presses play, the
  player re-fetches the link (a reload of the iframe with a fresh URL) instead of showing a dead player.
- Page title and Open Graph from the video title; `noindex` on the page, since the content is gated.

## Done when

- opening a free video's page plays it in our layout, in both locales, at 375 px and 1280 px
- a paid, unpublished or unknown video shows the right message, not a broken player
- no signed URL appears in the HTML of any other page once open item 1 is done
