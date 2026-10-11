# Plan 3 — Layout and nav

**Status: done.**

**Goal:** the shell every page shares, with the four routes in place.

## Header

| Item | Behaviour |
|---|---|
| Home | `/` |
| Categories | on `/`, smooth-scrolls to the first category section (`#categories`); from any other page, goes to `/#categories` |
| Courses | `/courses` |
| Login | `/login` |

- Sticky header; highlights the current route; mobile: hamburger menu with the same four
  items. Keyboard accessible, visible focus, `aria-current` on the active item.
- Anchor scroll offsets for the sticky header height (`scroll-margin-top`).
- Wordmark logo (open item 2; text in the display font until the SVG exists).
- **Language toggle** at the right of the bar: `VI | EN`, a real link to the same page in the
  other locale (`/courses` <-> `/en/courses`), current language marked `aria-current`. Works
  without JavaScript.

## Footer

Beige on olive. Contact block (hours, address, phones, email, Facebook, Instagram) from the
brand guide if open item 6 is a yes; otherwise copyright only. Brief/about content comes with
the later landing brief.

## Routes

- `/` landing (plan 4)
- `/courses` ([plan 5](05-courses-page.md)) and `/login` ([plan 6](06-login.md)) are real pages
  in this phase; until each lands, its route renders a bare heading so the nav works.
- Custom `not-found` and `error` pages.

## Done when

- all four nav items work on desktop and a 375px-wide viewport
- Categories scrolls with the offset on `/`, and lands on `/#categories` from `/courses`
- `/en/…` shows the English labels, `/…` Vietnamese, and the toggle switches between them
  keeping the path and query string
