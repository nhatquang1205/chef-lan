# Deploy the API — spec

Run the backend on the same VPS as wish-list and chef-lan-web, in Docker Compose, next to the
Postgres container that is already running, behind the nginx that already terminates HTTPS —
**on the same domain as the website, no separate API domain.**

## Plans

| # | Plan | Delivers | Status |
|---|---|---|---|
| 1 | [Production readiness](plans/01-production-readiness.md) | the code changes prod needs (first admin, proxy headers) | **done** |
| 2 | [Docker image](plans/02-docker-image.md) | `Dockerfile` that builds and runs the API | **done** |
| 3 | [Database](plans/03-database.md) | a `cheflan` database + user in the existing Postgres, schema applied | **done** (repo side; VPS step in `deploy/README.md`) |
| 4 | [Compose and nginx](plans/04-compose-and-nginx.md) | the service on the VPS, reachable at `https://<domain>/api/…` | **done** (files written; untested on the VPS) |
| 5 | [CI/CD](plans/05-ci-cd.md) | GitHub Actions: checks, then deploy on push to `main` | **done** (workflow written; first run pending) |
| 6 | [Web repo switch-over](plans/06-web-switch-over.md) | chef-lan-web points at the live API | todo |

## Target shape

```
browser ── https://<domain> ──> nginx :443
                                  ├─ /api/   ──> 127.0.0.1:5100  chef-lan API (container, :8080 inside)
                                  └─ /       ──> 127.0.0.1:3100  chef-lan-web (next start)
                       wish-list's own server_name block, untouched

chef-lan-web (server side) ── http://127.0.0.1:5100 ──> API      (skips nginx and TLS)
API container ── docker network ──> existing postgres container
```

## Decisions

| | |
|---|---|
| Public URL | **same domain**, path-based: nginx routes `/api/` to the API, everything else to the web app |
| CORS | **not needed in production** — the browser sees one origin. `Cors:AllowedOrigins` stays empty there; the Development policy for `localhost:3000` is unchanged |
| API port | host `127.0.0.1:5100` (loopback only, never public). Taken so far on this VPS: 3000 wish-list, 3100 chef-lan-web |
| Postgres | **reuse the running container**; the API joins its Docker network. A separate database and user for chef-lan, never wish-list's |
| Secrets | in a `.env` file on the VPS beside the compose file (gitignored); GitHub holds only the SSH deploy secrets. Never baked into the image |
| Environment | `ASPNETCORE_ENVIRONMENT=Production` — Scalar/OpenAPI off, no seeder, no sensitive-data logging |
| Migrations | production never runs `Database.Migrate()`; the idempotent SQL from `make script` is **applied automatically by every deploy**, before the new container starts |
| Image build | **GitHub Actions -> GHCR** (`ghcr.io/nhatquang1205/chef-lan-api:<sha>`); the VPS only pulls |
| First Admin | **opt-in startup seed**: `Seed__RunOnStartup=true` for the first deploy only, then removed |

## Why same-domain helps — and what it costs

**Helps:** no second certificate or DNS record, no CORS in production, and cookies/redirects
never cross origins. The web app's `NEXT_PUBLIC_API_BASE_URL` is just `https://<domain>`.

**Costs:** every `/api/...` path is reserved for the backend — the Next.js app must never add
its own `/api` routes (it has none). The admin endpoints (`/api/v1/admins/…`) are reachable on
the public site; they are protected by auth and rate limiting, and nginx can restrict them by IP
later if wanted.

## The easy-to-miss traps

1. **Rate limiting sees nginx, not the user.** Behind nginx and Docker, the API's
   `RemoteIpAddress` is the Docker bridge gateway, so **every visitor shares one IP bucket**:
   `auth-login` is 5/min *in total* and the whole site can be locked out of logging in. Fix
   (plan 1/4): nginx sends `X-Forwarded-For`, and `ForwardedHeaders:KnownProxies` lists exactly
   the gateway IP. Without `KnownProxies` it is worse than nothing (clients could forge their IP).
2. **There is no admin account in production.** The seeder only runs in Development. Plan 1
   decides how the first Admin gets created.
3. **Two different JWT signing keys**, at least 32 bytes each, generated fresh for production
   (`openssl rand -base64 48`, twice). Reusing the dev ones means anyone with the repo history
   could mint tokens.
4. **`SmtpEmailSender` is not implemented.** It is only registered outside Development and nothing
   sends email yet, so it is harmless today; the first feature that sends mail will throw.
5. **Postgres timezone/encoding** — the schema assumes `timestamptz` and UTC; the container
   already stores UTC, no change needed, but check `SHOW timezone;` on the shared instance.

## The VPS (answered)

| | |
|---|---|
| Postgres | container `postgres` (`postgres:17`), network **`postgres_default`**, published only on `127.0.0.1:5432` |
| Docker / Compose | 29.8 / v5.6 |
| Resources | 3.8 GB RAM (about 3 GB available, no swap), 11 GB disk free — fine for a runtime-only container; building in CI keeps the load off it |

## (original) Information needed from the VPS (run on the VPS, paste the output)

```bash
docker ps --format 'table {{.Names}}\t{{.Image}}\t{{.Ports}}'      # the postgres container's name and published ports
docker inspect <postgres-container> --format '{{json .NetworkSettings.Networks}}'   # which network(s) it is on
docker network ls
docker compose version && docker --version
free -h && df -h /                                                  # RAM and disk for the .NET build
```

## Open items

2. Database backups for the shared Postgres: out of scope here, but today there is none for
   chef-lan's data. Worth a nightly `pg_dump` before real customers arrive.
3. Make the GHCR package public after the first push (repo is public; the image has no secrets), so
   the VPS needs no registry login.

Decided: image build (GHCR), migrations (automatic), first admin (startup seed flag).

## Verified locally (before the first real deploy)

Built the image and ran it against a Postgres container with a **non-superuser database owner**,
through the real `deploy-api.sh`:

- `/health` 200; `/scalar` and `/openapi` 404 in Production; runs as uid 1654, cannot write to `/app`
- schema script applies cleanly and again with no changes (idempotent); tables owned by the app role;
  `CREATE EXTENSION citext` works without superuser
- `Seed__RunOnStartup=true` creates exactly one Admin however often it restarts; login works
- slug `Món Bắc` -> `mon-bac` inside the image (ICU present)
- **rate limiting**: without `KnownProxies` all clients share one login budget (client B was blocked by
  client A's attempts); with `KnownProxies` set to the network gateway each client has its own
- a deliberately broken deploy (missing JWT key) exits non-zero, prints the container's error and the
  rollback command

**Not verified:** anything on the VPS itself — nginx routing, GHCR pull, and the Docker gateway address
the API sees behind the published port on Linux (step 3 of "After the first deploy" in `deploy/README.md`).
