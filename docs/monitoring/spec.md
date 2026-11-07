# Monitoring

Why: API logs are only readable over SSH, nothing reports an error, and there is no view of the server's load or of
nginx traffic. This stays small: two containers on the VPS, no change to application code.

## Decisions

| | |
|---|---|
| Logs | **Dozzle**, watching `cheflan-api` only |
| Error alerts | Dozzle log alert to a **Discord** channel webhook, on every `Error` or `Critical` API log entry |
| Dashboard | **Netdata**: server CPU, RAM, disk, per-container usage, and nginx responses by status class (2xx, 3xx, 4xx, 5xx) parsed from the access log |
| Access | Both listen on `127.0.0.1` of the VPS only, reached through an SSH tunnel. No domain, no nginx change |
| Log format | JSON console in production, set by `Logging__Console__FormatterName=json` in the prod compose file, so each entry and its stack trace is one line that Dozzle can match on `message.LogLevel` |

Not used: Sentry (the user decided against it), Grafana and Prometheus (too much for one small VPS).

## Things that bite

- **Separate compose project.** `deploy-api.sh` runs `up -d --remove-orphans` in project `cheflan`: a monitoring service in
  that project would be deleted by the next deploy. Monitoring is project `cheflan-monitoring`, in `~/chef-lan-monitoring`.
- **Never publish a port on all interfaces.** The VPS shows no firewall we can rely on. Always `127.0.0.1:<port>:<port>`.
- **This repo is public.** The Discord webhook URL is a credential. It lives only in `~/chef-lan-monitoring/.env` and
  `notifications.yml` holds `${DISCORD_WEBHOOK_URL}`.
- **Discord refuses a message over its length limit**, so a long stack trace must be cut in the template or the alert is lost.
- **Log history in Dozzle is Docker's**: about 30 MB, and gone when a deploy replaces the container. Alerts are not affected.
- **The access log is shared.** Every site on this nginx writes to `/var/log/nginx/access.log`, so the status counts cover
  more than chef-lan. It is readable only by group `adm` (gid 4), hence `group_add` on Netdata.

## Plans

| # | Plan | Status |
|---|---|---|
| 01 | [Dozzle, Discord alerts, Netdata](plans/01-dozzle-discord-netdata.md) | todo |
