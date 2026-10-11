# Plan 1 — Repo scaffold

**Status: done.** Repo: `~/MyProjects/chef-lan-web`.

**Goal:** an empty Next.js app that builds, lints and runs, with the conventions fixed.

- `pnpm create next-app`: App Router, TypeScript (strict), ESLint, Tailwind, `src/` dir, `@/`
  import alias.
- Folder layout:
  ```
  src/app/[locale]/…        routes (locale segment, see plan 2)
  src/components/           shared UI (Header, Footer, Carousel, CourseCard)
  src/features/landing/     landing-only sections
  src/lib/api/              typed client, one file per backend area
  src/i18n/                 routing + messages (vi.json, en.json)
  ```
- `.env.example` with `API_BASE_URL=http://localhost:5059`; real `.env.local` gitignored.
  Server-side calls use `API_BASE_URL`; only add `NEXT_PUBLIC_` for values the browser needs.
- Scripts: `dev`, `build`, `lint`, `typecheck`.
- `README.md` (how to run against the local API) and a `CLAUDE.md` for the new repo.
- **Theme tokens** in the Tailwind config from the spec's Brand table: `olive` `#2b2a16`,
  `beige` `#d3c8b6`, `terracotta` `#7a3f21`. Dark theme is the only theme. Display font and
  body font as CSS variables (`next/font/local` once the files exist — open item 1).
- `next/image` remote patterns for the thumbnail host (Bunny CDN / wherever thumbnails live).

## Done when

- `pnpm dev` serves a page at `http://localhost:3000`
- `pnpm build`, `pnpm lint` and `pnpm typecheck` all pass clean
- nothing secret is committed; `.env.example` is
