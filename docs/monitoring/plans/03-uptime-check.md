# Plan 03: outside uptime check from GitHub Actions, alerts to Discord

Why: Dozzle reports API errors, but if the VPS, nginx, the web server or the API process dies nothing is logged and nobody
is told. The check must run outside the VPS.

## Decisions (confirmed)

| | |
|---|---|
| Runner | A scheduled GitHub Actions workflow in **this repo** (public, so minutes are free) |
| Interval | `*/5 * * * *` (GitHub often starts scheduled runs late, 5 to 15 minutes; accepted) plus `workflow_dispatch` |
| Checks | `https://courses.bonbocon.com/` 200, `https://admin.bonbocon.com/` 200 and contains `<div id="root">`, `https://api.bonbocon.com/api/v1/health` 200 (`/health` is not public) |
| Alerts | **Only when the state changes**: one message when a site goes down, one when it is back (with how long it was down). Same Discord channel as Dozzle |
| Flapping | A site is down only after 3 failed tries 20 s apart (timeout 15 s each) |

## Build

1. `.github/workflows/uptime.yml`:
   - `on: schedule` + `workflow_dispatch`; `permissions: { actions: read, contents: read }`; `concurrency: uptime` (no overlap,
     do not cancel in progress); `timeout-minutes: 5`.
   - **State between runs** = a tiny artifact `uptime-state` (JSON: per site `up`/`down` and `since`), uploaded every run with
     `retention-days: 2`. The run downloads the one from the latest previous run that has it (`gh run list --workflow uptime.yml
     --status completed` then `gh run download <id> -n uptime-state`, with `GH_TOKEN: ${{ github.token }}`). No state found =
     everything was up.
   - The job itself **succeeds when a site is down** (the down state is data, not a job failure). It fails only if the check
     script breaks. So GitHub does not e-mail a failure every 5 minutes during an outage.
   - Discord via `curl` with the repo secret `DISCORD_WEBHOOK_URL` (`allowed_mentions: {parse: []}`, JSON built with `jq`).
     Messages: `🔴 courses.bonbocon.com không truy cập được (HTTP 522)` / `🟢 courses.bonbocon.com đã hoạt động lại sau 12 phút`.
     Several sites changing in one run = one message. If the secret is missing, the step logs a notice and skips the post.
   - The check logic is a script `.github/scripts/uptime-check.sh` (bash + curl + jq, all present on `ubuntu-latest`) so it can
     be run locally: sites and state file path as arguments/env, and a `DRY_RUN=1` that prints the message instead of posting.
2. Keep-alive: GitHub disables scheduled workflows in a public repo after 60 days without commits. Note it in the README
   section with the fix (Actions tab -> enable workflow). No keep-alive commits.
3. Docs: `deploy/README.md` Monitoring gets "Uptime check" (add the secret: repo Settings -> Secrets and variables -> Actions ->
   `DISCORD_WEBHOOK_URL`, the same webhook URL; run it once with "Run workflow"); `docs/monitoring/spec.md` decisions table gets a
   row and this plan's status row.

## Done when

`actionlint` clean (via `docker run --rm -v "$PWD:/repo" -w /repo rhysd/actionlint:latest`), `shellcheck` clean. The script run
locally with `DRY_RUN=1`: all up with no state -> no message; a site pointed at a dead URL (e.g. `https://127.0.0.1:9/`) -> one
down message and state `down`; run again still down -> no message; back to a good URL -> one recovery message with the duration.
The real webhook is never called during development.

## Implementation notes

Built: `.github/workflows/uptime.yml` and `.github/scripts/uptime-check.sh`. actionlint and shellcheck clean. Not run on GitHub yet.

- **API URL changed:** the plan said `https://api.bonbocon.com/health`, but nginx deliberately blocks `/health` publicly (404;
  `deploy/nginx/api.bonbocon.com`). The check uses `https://api.bonbocon.com/api/v1/health` (liveness, 200). It does not prove the
  database is reachable; it proves nginx, Cloudflare and the API process are up.
- **State** is `{ "<host>": { "state": "up"|"down", "since": <epoch seconds> } }`, artifact `uptime-state`, 2-day retention.
  The workflow walks the last 15 completed runs (skipping the current one) and takes the first that still has the artifact;
  failed, cancelled or expired runs are skipped; none found = all up. The upload runs `if: always()`.
- **Sites are checked in parallel**, so the worst case (3 tries x 15 s + 2 x 20 s) stays well under the 5-minute job limit.
- **A failed Discord post** exits 1 and leaves the previous state unwritten, so the next run alerts again. (A missing secret only
  logs a notice and still records the state.)
- Script env: `STATE_FILE`, `DISCORD_WEBHOOK_URL`, `DRY_RUN=1`, `SITES` (lines `name|url|expected text`), `TRIES`, `RETRY_DELAY`,
  `TIMEOUT`, `NOW`. Local run: `DRY_RUN=1 STATE_FILE=/tmp/s.json .github/scripts/uptime-check.sh`.
- Verified locally (DRY_RUN): all up/no state = no message; dead URL = one down message, state `down`; second run = no message;
  good URL again = one "đã hoạt động lại sau 15 phút" message; wrong body text = down; a real post to a local listener sent
  `{content, allowed_mentions: {parse: []}}`. The real webhook was never called.

### Text for `deploy/README.md`, Monitoring, "Uptime check"

> **Uptime check.** `.github/workflows/uptime.yml` runs every 5 minutes (GitHub may start it 5 to 15 minutes late) from outside the
> VPS and checks `courses.bonbocon.com/`, `admin.bonbocon.com/` (must contain `<div id="root">`) and `api.bonbocon.com/api/v1/health`.
> A site counts as down after 3 failed tries 20 s apart. It posts to Discord only when a site goes down and when it is back
> (with the downtime). One-time setup: repo Settings -> Secrets and variables -> Actions -> new secret `DISCORD_WEBHOOK_URL`
> (the same webhook as Dozzle), then Actions -> Uptime -> Run workflow once. The job succeeds even while a site is down; state
> lives in the `uptime-state` artifact (2 days). **GitHub disables scheduled workflows after 60 days without commits to the repo**:
> if the alerts stop, Actions -> Uptime -> Enable workflow. Locally: `DRY_RUN=1 .github/scripts/uptime-check.sh`.

### Row for the `docs/monitoring/spec.md` decisions table

> | Uptime check | Scheduled GitHub Actions workflow (every 5 min, outside the VPS), alerts on state change only to the Dozzle Discord channel; state in a 2-day artifact; probes `/api/v1/health` because `/health` is not public. Plan 03 |

Plan status row: `03 uptime check | built, not yet run on GitHub (add the DISCORD_WEBHOOK_URL secret first)`.
