# Plan 2 — `ICurrentUser` middleware (moved)

**This plan moved to [`docs/auth/plans/01-current-user.md`](../../auth/plans/01-current-user.md).**

`ICurrentUser` is authentication infrastructure, and the auth feature now lands before the
content CRUD plans that depend on it. Keeping it numbered here would have interleaved the two
features awkwardly.

Nothing is lost — the content is identical, including the CLAUDE.md rule it supersedes and
the middleware ordering trap. This file is a stub so the numbering in
[spec.md](../spec.md) stays stable and old links do not dangle.
