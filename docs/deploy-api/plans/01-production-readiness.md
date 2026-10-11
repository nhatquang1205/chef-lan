# Plan 1 — Production readiness

**Status: done.** `Seed:RunOnStartup` implemented in `Program.cs`; proxy trust is `ForwardedHeaders__KnownProxies__0` (compose pins the network gateway). Code changes in this repo; nothing deploys yet.

## 1. The first Admin account

Production starts with an empty `identity.users`, and the seeder (Development only) cannot help.
**Decided: option A.** Options considered:

| | How | Trade-off |
|---|---|---|
| **A. Opt-in startup seed (recommended)** | the seeder also runs when `Seed:RunOnStartup=true`, creating the Admin from `Seed:AdminEmail` / `Seed:AdminPassword` only if no Admin exists. Set it for the first deploy, then remove it | tiny change; idempotent; the password sits in the VPS `.env` until removed |
| B. One-off SQL | insert the row by hand with a bcrypt hash generated locally | no code change; easy to get a column wrong; hash tooling needed |
| C. A CLI command in the image | `docker compose run --rm api create-admin …` | cleanest, but a new command-line surface to build |

Whatever the choice: the seeded Admin has `must_change_password = true`; log in once and call
`POST /api/v1/admins/auth/change-password`. The seed must **never** run unless explicitly enabled.

## 2. Proxy headers (rate limiting)

`UseForwardedHeaders` is already wired and only turns on when `ForwardedHeaders:KnownProxies` is
set. For production set it to the **Docker network gateway** nginx arrives from
(`ForwardedHeaders__KnownProxies__0=172.x.0.1`, found with `docker network inspect`). Verify after
deploy that two different visitors do not share one login budget.

## 3. Container-friendly config

- listen on `http://+:8080` (`ASPNETCORE_URLS`), no HTTPS inside the container (nginx terminates it)
- `/health` stays unauthenticated for the deploy check, but is **not** routed by nginx
  (`/api/` only), so it is not public
- Serilog already writes to the console; Docker captures it (set log rotation in the compose file)

## Done when

- with `ASPNETCORE_ENVIRONMENT=Production` and the env vars set, the API starts, `/health` is 200,
  and `/scalar` and `/openapi` are 404
- with `Seed:RunOnStartup=true` an Admin exists exactly once, however many times it restarts;
  without it nothing is seeded
- a request with `X-Forwarded-For` from a trusted proxy is rate-limited per *client* IP
