# Plan 4 — Landing page

**Status: done.** Needs plans 2 and 3, and a few published courses in the backend to look
right.

**Goal:** `/` shows a hero slider and one carousel section per category.

## Hero slider

- Slides from `GET /api/v1/courses?pageSize=5` (newest published): thumbnail background,
  title, subtitle, price, and a button to the course.
- Autoplay with pause on hover/focus, dots and arrows, swipe on touch, and
  `prefers-reduced-motion` stops autoplay.
- Zero courses -> the hero is **not rendered** (no placeholder slide). Missing thumbnail ->
  olive gradient with the pasta-line texture as fallback.
- The first slide image is `priority` loaded (it is the LCP element).

## Category sections

- `GET /api/v1/categories`, then per category `GET /api/v1/courses?categorySlug=<slug>&pageSize=5`.
- Each section is `<section id=…>`; the first carries the `#categories` anchor the nav
  targets. Heading = category name.
- A carousel of up to **5** `CourseCard`s, then a **Show more** button to
  `/courses?category=<slug>`.
- Categories with no published courses are not rendered.
- `CourseCard`: thumbnail (fallback), title, subtitle, level, lesson count and duration,
  `priceDisplay` with strikethrough `compareAtPrice` when set. Links to `/courses/<slug>`
  (open item 5: the detail page is not in this phase, so decide before building cards).

## Carousel

Use `embla-carousel-react` for both the hero and the category rows: small, headless, no
styling opinions, SSR-safe. One shared `Carousel` component wrapping it.

## Rendering

Server components with ISR (`revalidate = 60`). The landing page is the SEO entry point, so
the HTML must contain the categories and course titles without JavaScript.

## Done when

- `/` renders the hero and every non-empty category section from the running API
- showing 5 cards per carousel, swipe/arrows work, and Show more goes to
  `/courses?category=<slug>`
- a course with no thumbnail, a category with no courses, and the API being down all render
  sensibly
- Lighthouse: no layout shift from images, LCP image prioritised, accessibility >= 90
- viewed at 375px, 768px and 1280px
