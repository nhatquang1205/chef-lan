# Customer web — spec

The public storefront for chef Lan's courses: a landing page, a course list, and login.
A **separate repository** from this backend; this folder holds the plan because the backend
is the contract it builds against.

## Plans

| # | Plan | Delivers | Status |
|---|---|---|---|
| 0 | [Backend: CORS](plans/00-backend-cors.md) | browser calls to the API allowed | **done** |
| 1 | [Repo scaffold](plans/01-repo-scaffold.md) | Next.js + TS repo, tooling, env, CI-ready | **done** |
| 2 | [API client and i18n](plans/02-api-client-and-i18n.md) | typed API layer, vi/en strings | **done** |
| 3 | [Layout and nav](plans/03-layout-and-nav.md) | header with 4 items, footer, routes | **done** |
| 4 | [Landing page](plans/04-landing-page.md) | hero slider + one carousel section per category | **done** |
| 5 | [Courses page](plans/05-courses-page.md) | `/courses` list with search, filters, paging | **done** |
| 6 | [Login](plans/06-login.md) | `/login` form, session, header state, logout | **done** |

## Decisions (confirmed)

| | |
|---|---|
| Framework | **Next.js**, App Router, TypeScript |
| Repo | **`chef-lan-web`**, at `~/MyProjects/chef-lan-web` |
| Styling | **Tailwind CSS**, no component library (assumed — answer 2 was blank) |
| Languages | **Vietnamese + English**, with a **visible toggle in the nav bar** (UI chrome only, see below) |
| Brand | **Bon Bocon**, from the brand guide — see [Brand](#brand) |
| Nav | 4 items: **Home**, **Categories** (scrolls to the category sections), **Courses** (`/courses`), **Login** (`/login`) |
| Hero slider | **Featured published courses** from `GET /api/v1/courses` — no backend change |
| Category sections | one section per category; carousel of **5** courses; **Show more** -> `/courses?category=<slug>` |
| Empty hero | no courses -> the hero is simply not rendered (no placeholder slide) |
| `/courses`, `/login` | **real pages**, not placeholders — plans 5 and 6 |
| Out of scope | My courses, enrollments, playback, checkout, the landing "brief" (comes later) |

## What the backend gives us today

| Need | Endpoint |
|---|---|
| Hero slides | `GET /api/v1/courses?pageSize=5` (newest published first) |
| Category sections | `GET /api/v1/categories` (Active only, by display order) |
| Courses in a section | `GET /api/v1/courses?categorySlug=<slug>&pageSize=5` |
| Course list page | `GET /api/v1/courses` with `q`, `categorySlug`, `level`, `sort`, `page`, `pageSize` |
| Login (later) | `POST /api/v1/auth/login`, `refresh`, `logout`, `GET /me` |

Enums come back as **strings** (`"level": "Beginner"`). Course cards have: `slug`, `title`,
`subtitle`, `thumbnailUrl`, `level`, `priceVnd`, `priceDisplay`, `compareAtPriceVnd`,
`lessonCount`, `totalDurationSeconds`, `categories[]`.

The FE URL is `?category=<slug>`; the API parameter is `categorySlug`. The FE maps it.

## Things to know before building

- **Content is Vietnamese only.** Category names, course titles and descriptions come from
  the API as chef Lan typed them. The English locale translates the UI chrome (menu, buttons,
  labels), not the catalog. Translating the catalog would need backend fields — not planned.
- **N+1 on the landing page.** One categories call plus one courses call per category
  (about 7 requests for the 6 starter categories). Acceptable because the page is rendered on
  the server and cached (ISR, ~60 s); revisit with a single "home" endpoint if categories grow.
- **Empty categories are hidden** on the landing page — a section with no courses is noise.
- **Thumbnails may be missing** (`thumbnailUrl` is nullable). Every card and slide needs a
  fallback image.
- **No CORS on the backend yet.** Server-side fetches in Next.js do not need it; the browser
  calls (login, later) do. Plan 0 adds it.
- **Local API URL** is `http://localhost:5059`; the base URL is an env var, never hardcoded.
- Prices are whole dong; use `priceDisplay` from the API (`1.200.000 ₫`) rather than
  re-formatting.

## Brand

From page 3 of `BON BOCON - BRAND GUIDE.pdf` (colors sampled from the PDF).

| Token | Value | Use |
|---|---|---|
| Main | `#2b2a16` (dark olive) | page background, header, footer |
| Secondary 1 | `#d3c8b6` (beige) | text on dark, the wordmark, cards on dark |
| Secondary 2 | `#7a3f21` (terracotta) | accents: buttons, links, active nav, badges |

The brand is a **dark theme**: beige on dark olive, terracotta as the accent. Beige on
`#2b2a16` is high contrast (about 9:1); terracotta text on olive is **not** (about 1.8:1), so
terracotta is for button/badge **backgrounds** with beige text, never for body text.

Fonts named in the guide's PDF: **GLANCE** (display/titles — the logo and "HOMEMADE HEART
ITALIAN ART" lettering) and **Calps Sans** (content, Light and Bold). Neither is a standard
web font; see open item 1.

Logo: the "Bon Bocon / ITALIAN HOMEMADE RESTAURANT" wordmark in beige on olive. The pasta-line
illustration is a background texture; plan uses it sparingly (hero/footer) at low opacity.

## Resolved decisions

| | |
|---|---|
| Fonts | **Google Fonts stand-ins**: Yeseva One (display) and Barlow Semi Condensed (body), both with the Vietnamese subset. The brand fonts GLANCE / Calps Sans are commercial; swap in with `next/font/local` if the files turn up |
| Logo | the square logo PNG supplied (header uses a text wordmark in the display font; footer and favicon use the PNG) |
| Session storage | **localStorage + CORS** (not httpOnly cookies). Backend `Cors:AllowedOrigins`; `http://localhost:3000` in Development |
| Register page | **built** (`/register`) |
| Course detail page | **built**, minimal (`/courses/{slug}`) |
| Footer | contact details from the brand guide (hours, address, phones, email, Facebook, Instagram) |

## What was built differently from the plans

- **No `next-intl`.** Hand-rolled dictionaries and `next.config` rewrites (not a `proxy.ts`: that 500s behind nginx), following the Next.js 16
  internationalization guide. Fewer moving parts on a brand-new major version.
- **No generated API types.** The OpenAPI document types every `int64` as `number | string`,
  which would force a cast at every use. Types are hand-written in `src/lib/api/types.ts`.
  (This did expose a real backend bug, fixed: OpenAPI described enums as numbers while the
  API sends strings — `ConfigureHttpJsonOptions` now also registers the string-enum converter.)
- **Filters are a plain GET form**, not client state, so they work without JavaScript and every
  filter is in the URL.
- **Landing data is a request-time hole** (`expire` < 5 min under Cache Components), so the
  build does not depend on the API.

## Known gaps

- `/courses/{unknown}` answers HTTP 200 with the not-found page (streaming starts before
  `notFound()` can set the status). Soft-404 for crawlers.
- The social links in the footer are inferred from the handle (`facebook.com/<handle>`);
  confirm they are the real pages.
- No automated tests in the web repo.
