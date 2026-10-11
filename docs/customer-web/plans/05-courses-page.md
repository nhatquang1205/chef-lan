# Plan 5 — Courses page

**Status: done.** Needs plans 2 and 3. The landing page's **Show more** buttons point here.

**Goal:** `/courses` lists published courses, with filters that live in the URL.

## URL contract

`/courses?category=<slug>&q=<text>&level=<Beginner|Intermediate|Advanced>&sort=<…>&page=<n>`

- `category` is what the landing page links to; the FE sends it to the API as `categorySlug`.
- `sort`: `Newest` (default), `PriceAsc`, `PriceDesc`, `Title` — the API's own values.
- All state is in the URL, so a filtered list is shareable, bookmarkable and works with the
  back button. Server component reads `searchParams`; no client state to sync.

## Page

- Header strip: title, and the active category name when filtered (from `GET /categories`).
- Filter bar: search box (submit on enter / debounce), category select (Active categories),
  level select, sort select, **Clear** when anything is set.
- Grid of `CourseCard` (the same component as the landing page), 12 per page.
- Pagination from the API's `page`, `totalPages`, `hasNext`, `hasPrevious`; page links keep the
  other filters.
- Empty state ("no courses match") with a Clear button; error state when the API is down.
- `q` search is accent-**sensitive** on the backend: `pho` does not find `Phở`. Known backend
  gap (CLAUDE.md); the empty state can hint at typing with accents. Not fixed here.

## Done when

- `/courses` lists the first page; `?category=banh-ngot` filters; unknown category slug shows
  the empty state, not an error
- changing any filter resets `page` to 1; page links keep filters
- `/courses?q=%` returns nothing (the backend escapes it) and does not crash
- the same page works at `/en/courses` with English labels
- 375px layout: filters stack, grid is one column
