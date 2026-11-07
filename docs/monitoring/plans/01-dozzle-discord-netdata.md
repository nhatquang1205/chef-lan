# Plan 01: Dozzle, Discord alerts, Netdata

Read `../spec.md` first.

## Build

1. `deploy/docker-compose.prod.yml`: add `Logging__Console__FormatterName: json` to the api environment.
2. `deploy/monitoring/docker-compose.yml`, top-level `name: cheflan-monitoring`. Pin exact image versions, never `latest`.
   - `dozzle`: `amir20/dozzle`, `127.0.0.1:8081:8080`, `/var/run/docker.sock` read-only, `./dozzle-data:/data`,
     `DOZZLE_FILTER=name=cheflan-api`, `DISCORD_WEBHOOK_URL` from `.env`, the same log rotation as the API, `restart: unless-stopped`.
   - `netdata`: `netdata/netdata`, on a normal bridge network with `127.0.0.1:19999:19999` (NOT `network_mode: host`: a missed
     bind setting would make it public). Follow the official Docker install for the mounts, but mount only
     `/var/log/nginx:/host/var/log/nginx:ro`, not all of `/var/log`. `group_add: ["4"]` for the access log, `DO_NOT_TRACK=1`.
     Named volumes for its lib and cache. Limit stored metrics to at most 500 MB of disk using the setting for the pinned version.
3. `deploy/monitoring/netdata/go.d/web_log.conf`: one job for `/host/var/log/nginx/access.log`, mounted read-only. No Netdata
   notification is configured: Discord only gets API errors.
4. `deploy/monitoring/notifications.yml`, seeded into Dozzle's `/data`: a Discord webhook destination using `${DISCORD_WEBHOOK_URL}`
   and one log alert. Container: `name == "cheflan-api"`. Log: `message.LogLevel == "Error" || message.LogLevel == "Critical"`.
   The message shows level, category, message and the start of the exception, cut well under Discord's limit, and stays valid
   JSON when the text holds quotes and newlines. Work out the file format from a real Dozzle of the pinned version.
5. `deploy/monitoring/.env.example` and a "Monitoring" section in `deploy/README.md`: setup on the VPS without sudo (the deploy
   user is in group `docker`), creating the Discord webhook, updating, and the tunnel:
   `ssh -N -L 8081:127.0.0.1:8081 -L 19999:127.0.0.1:19999 nina`. Replace the Logs row under Operations. Add a short Monitoring
   section to `CLAUDE.md`.

## Done when (local Docker only)

- The API runs in a local container with the JSON setting, and an unhandled exception writes one JSON line holding the stack trace.
- A local HTTP receiver stands in for Discord: one error produces one POST with valid JSON under the limit; a `Warning` or a
  4xx produces none; an exception with quotes, newlines and a stack trace over 4,000 characters is still delivered.
- A burst of 50 errors: report how many POSTs arrive. Dozzle documents no cooldown for log alerts.
- Recreating the API container under the same name keeps alerts working.
- Netdata shows host CPU and RAM and per-container usage, and counts a sample combined-format access log by status class.
- `docker compose config` passes for both files. Nothing listens on a non-loopback address. No webhook URL in the repo.
- Note what could not be proven outside Linux, such as host metrics under Docker Desktop.

## Not in scope

Container-down alerts, CPU or RAM alerts, the web app's logs, longer log history, a public domain.

## Implementation notes

Tested locally on Docker Desktop (Linux VM), pinned `amir20/dozzle:v11.4.1` and `netdata/netdata:v2.12.3`. The VPS steps are in
`deploy/README.md`, "Monitoring". Nothing was deployed.

Changed from the plan:

- **Log format.** `Logging__Console__FormatterName=json` does nothing: `Program.cs` uses Serilog (`UseSerilog` + `ReadFrom.Configuration`) and
  replaces the built-in console logger. The compose environment sets
  `Serilog__WriteTo__0__Args__formatter=Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact` instead (the
  assembly ships with Serilog.AspNetCore; no code change). `Serilog.Formatting.Json.JsonFormatter` was tried first: its `renderMessage`
  option cannot be set from configuration, so entries carried only the message template. The compact fields are `@l` level, `@m` message,
  `@x` exception, `SourceContext`.
- **Levels.** Serilog calls Critical `Fatal`, so the alert is `message["@l"] == "Error" || message["@l"] == "Fatal"`.
- **Template syntax.** Dozzle parses the template as a Go template before it reads it as JSON, so a `\"` inside an action is a parse error and the
  dispatcher is silently skipped ("Skipping invalid dispatcher" in Dozzle's log). Actions use backticks. Because the text is then valid JSON,
  Dozzle escapes quotes and newlines in the inserted values itself.
- Added `netdata/netdata.conf` (only the three `dbengine tier N retention size` lines: 300 + 100 + 100 MiB = 500 MiB, default is 1 GiB per tier) and
  `DOZZLE_NO_ANALYTICS=true`. Kept the official `/:/host/root:ro,rslave` mount, which Netdata uses for host disk space; only `/var/log` is narrowed.
- A webhook to a loopback or link-local address is refused by Dozzle (SSRF guard), so the local receiver ran as a container on the compose network.

Results:

- One failing request logs four `Error` entries (EF connection, EF query, `RequestLoggingMiddleware`, `GlobalExceptionHandler`) and so sends four
  messages. An unhandled exception is one JSON line (about 6.5 KB of `@x`).
- Burst of 50 `Error` entries: 50 POSTs, none dropped or merged. Discord rate-limits webhooks, so against the real service some would get a 429
  and Dozzle does not retry.
- Content is about 1.4 KB for a 6,000 character exception with quotes, backslashes, tabs and newlines; always valid JSON, and under 2,000 by construction.
- Not proven here: host CPU/RAM/disk are those of the Docker Desktop VM; per-container charts exist but are keyed by container id, not name
  (name resolution failed in the Desktop VM's cgroup layout, expected to work on a normal Linux host); `group_add: "4"` against a real `root:adm 640`
  access log; the real Discord API (webhook response, rate limit, content limit); `rslave` on `/`; the nginx log format on the VPS (`log_type: auto`
  needs at least one line in the file when Netdata starts, otherwise the job fails its check until a restart).
