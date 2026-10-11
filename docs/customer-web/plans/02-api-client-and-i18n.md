# Plan 2 — API client and i18n

**Status: done.**

**Goal:** a typed way to call the backend and a way to show strings in two languages, before
any page needs them.

## API client

- Types **generated from the backend's OpenAPI document** (`/openapi/v1.json`, served in
  Development) with `openapi-typescript`, committed under `src/lib/api/schema.d.ts`, plus a
  script to regenerate. No hand-written response types to drift.
- `src/lib/api/client.ts`: a small `fetch` wrapper — base URL from `API_BASE_URL`, JSON in/out,
  throws a typed `ApiError` carrying the backend's `ProblemDetails` (`status`, `title`,
  `detail`, `code`).
- Functions for this phase only: `listCategories()`, `listCourses({ categorySlug, pageSize,
  page, q, level, sort })`. Cached with Next's `fetch` revalidate (60 s) for landing data.
- Backend unreachable must not crash the page: callers get an error they can render around.

## i18n

- `next-intl`, locales `vi` (default) and `en`. `localePrefix: 'as-needed'` so Vietnamese
  lives at `/courses` and `/login` exactly as specified, English at `/en/courses`.
- Messages in `src/i18n/messages/{vi,en}.json`. Only UI chrome is translated; catalog data is
  Vietnamese from the API.
- Dates/numbers via `Intl` with the active locale; money uses the API's `priceDisplay`.

## Done when

- `pnpm typecheck` fails if the generated schema and usage disagree
- a throwaway page renders the categories from the running API, in both locales
- with the API stopped, that page shows an error state instead of a 500
