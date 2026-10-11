# Plan 2 — Docker image

**Status: done.** `Dockerfile` + `.dockerignore`; the legacy local builder was used, so `# syntax` is a no-op there but fine in CI (buildx).

- `Dockerfile` at the repo root, multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` to restore and
  `dotnet publish src/ChefLan.Courses.API -c Release --no-restore`, then the smaller
  `mcr.microsoft.com/dotnet/aspnet:10.0` runtime.
- Restore first from `*.csproj`, `Directory.*.props`, `global.json` and `nuget` config so the layer
  caches until a dependency changes. `global.json` pins SDK `10.0.400` with `rollForward:
  latestFeature`, so the SDK image must be at least that; check the tag actually satisfies it.
- Run as a **non-root** user; `EXPOSE 8080`; no secrets, no `appsettings.Local.json`, no `.env` in
  the image (`.dockerignore` excludes them, plus `bin/`, `obj/`, `.git`, `docs/`).
- The runtime image has no `curl`; the container health check, if any, uses a tiny TCP/`dotnet`
  probe or is done from the deploy script instead.
- `InvariantGlobalization` stays **off** (the slug generator has an explicit Vietnamese map, but
  other culture-aware code should behave); the ICU package must be present in the runtime image —
  `aspnet:10.0` (Debian/Ubuntu) includes it, an Alpine variant needs `icu-libs`. Verify the slug
  `Món Bắc -> mon-bac` on the built image (see CLAUDE.md § Slugs).

## Done when

- `docker build -t chef-lan-api .` succeeds from a clean checkout
- `docker run` against a local Postgres (with env vars) answers `/health` 200
- the image contains no files from `.env`, `appsettings.Local.json` or `docs/`
- `docker run … sh` as the non-root user cannot write outside its temp dir
