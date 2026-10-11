# Plan 5 — CI/CD

**Status: done** (`.github/workflows/deploy.yml`: check -> image -> deploy); first run pending. Mirrors chef-lan-web's workflow (build, then SSH deploy).

## Checks (every push and PR)

`dotnet build ChefLan.slnx --no-incremental -warnaserror` (the repo is kept at 0 warnings) and, once
they exist, the tests. Also `docker build` to prove the Dockerfile still works. Generate
`cheflan-courses.sql` with `dotnet ef migrations script --idempotent` and upload it as an artifact.

## Deploy (push to `main`, after the checks)

Environment `production`; secrets `SSH_PRIVATE_KEY`, `VPS_HOST`, `VPS_USER`, `VPS_PORT` (optional).
On the VPS, in order:

1. get the new code or image (below)
2. apply the SQL (plan 3, if automatic)
3. `docker compose -f docker-compose.prod.yml up -d` (recreates the container)
4. health check `http://127.0.0.1:5100/health`; on failure print `docker compose logs --tail=50 api`
   and **exit non-zero**
5. `docker image prune -f`

**Rollback:** the previous image stays tagged (`:previous`) until the next deploy, so
`docker compose up -d` with that tag restores it in seconds. Database changes are forward-only:
write migrations to be backwards-compatible for one release.

## Where the image is built — decided: GHCR

| | |
|---|---|
| **On the VPS** (like chef-lan-web) | the deploy `git reset --hard`s the repo and runs `docker compose build`. No registry, no extra secrets. Costs ~1 GB RAM and a minute or two of CPU on the shared box during every deploy |
| **In GitHub Actions -> GHCR (chosen)** | CI builds and pushes `ghcr.io/<you>/chef-lan-api:<sha>`; the VPS only `pull`s. Needs a registry login on the VPS (a read-only token). Keeps the VPS light, gives immutable per-commit tags and trivial rollbacks |

Concurrency: one deploy at a time; a running deploy is never cancelled (same as the web repo).

## Done when

- a PR runs the checks but never touches the VPS
- a push to `main` ends with the health check green, and a deliberately broken push leaves the
  previous container running and the workflow red
- the generated SQL is downloadable from the run

## GHCR specifics

- CI logs in with the built-in `GITHUB_TOKEN` (`packages: write`) and pushes
  `ghcr.io/nhatquang1205/chef-lan-api:<git-sha>` plus `:latest`; the deploy uses the **sha** tag so
  every deploy is reproducible and rollback is "deploy the previous sha".
- The package is private by default. The VPS needs `docker login ghcr.io` once with a classic PAT
  that has only `read:packages` (stored in `~/.docker/config.json`), or make the package public
  (fine only because the image contains no secrets).
- The compose file reads the tag from an `IMAGE_TAG` variable the deploy sets, instead of building.
