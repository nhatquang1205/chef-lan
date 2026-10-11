# Plan 3 — Preview limit (`freeTillSeconds`)

**Status: done.** `chef-lan-web`. Needs plan 2. Applies only when `freeTillSeconds` is not null.

Client-side, through Bunny's player.js API on the iframe:

- on `timeupdate`: if `seconds >= freeTillSeconds` -> `pause()`, `setCurrentTime(freeTillSeconds)`, show the
  end-of-preview overlay
- on a seek past the limit (a `timeupdate` jump or `seeked`): set the time back to just under the limit
- the overlay sits over the player: "Bạn đã xem hết phần xem thử" / "The preview has ended", a link back to
  the course, and the contact line used on the course page ("Contact chef Lan to enroll")
- a small label while playing: "Xem thử: còn mm:ss" so the limit is not a surprise
- `freeTillSeconds = null` -> none of this runs; the whole video plays

**It is a teaser limit, not a paywall.** The signed link is for the full video, so opening it outside the
page, or editing the DOM, gets past the check (see the spec for what makes that harder and what cannot be
fixed without a separately encoded preview clip). Anything that must stay paid has `isFree = false`.

## Done when

- a video with `freeTillSeconds = 120` stops at 2:00 with the overlay, and dragging to 5:00 snaps back
- a video with `freeTillSeconds = null` plays to the end; a paid video never reaches this page
- the overlay text and the countdown exist in both locales
- reloading at 1:59 does not grant another 2 minutes by itself being a fresh page: it is the same
  limit (the limit is on position, not on time spent)
