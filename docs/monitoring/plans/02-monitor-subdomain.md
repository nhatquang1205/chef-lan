# Plan 02: monitor.bonbocon.com behind basic auth

Why: reaching Dozzle and Netdata needed an SSH tunnel. They now have one subdomain, protected by nginx basic auth.

## Decisions (confirmed)

| | |
|---|---|
| Host | `monitor.bonbocon.com`: Netdata at `/system/`, Dozzle at `/logs/`, `/` redirects to `/system/` |
| Auth | nginx basic auth, one user, `/etc/nginx/.htpasswd-monitor` created by the user on the VPS (the password never leaves their terminal) |
| Rate limit | `limit_req` per visitor IP (real IP from `cloudflare-realip.conf`), 20 r/s, burst 200: the dashboards poll a lot, so this caps abuse rather than guessing; a long random password is the real defence |
| Containers | Unchanged ports, still `127.0.0.1` only. Dozzle gets `DOZZLE_BASE=/logs`; Netdata has no base path, nginx strips `/system/` |
| Cloudflare | Proxied like the other hosts; Dozzle's server-sent events go through with `proxy_buffering off` |

## Tested

On the VPS, with a throwaway nginx container on 127.0.0.1 and an SSH tunnel: Netdata under `/system/` loads the agent console
and the full dashboard (no failed requests, deep links reload); Dozzle under `/logs/` lists `cheflan-api` and streams its log.
Dozzle was restarted on the VPS with `DOZZLE_BASE=/logs` (the tunnel URL is now `localhost:8081/logs/`).

## VPS steps (user, sudo)

1. Cloudflare: A record `monitor` → 171.244.60.174, DNS only until certbot has run, then proxied.
2. Password file: `openssl rand -base64 24` (keep it in a password manager), then
   `printf 'admin:%s\n' "$(openssl passwd -apr1)" | sudo tee /etc/nginx/.htpasswd-monitor >/dev/null`
   `sudo chown root:www-data /etc/nginx/.htpasswd-monitor && sudo chmod 640 /etc/nginx/.htpasswd-monitor`
3. `sudo cp ~/nginx-bonbocon/monitor-ratelimit.conf /etc/nginx/conf.d/`, the site file to `sites-available`, link it, `nginx -t`,
   reload, `sudo certbot --nginx -d monitor.bonbocon.com`.

## Verify

Without credentials: 401 on `/`, `/system/`, `/logs/`. With them: both dashboards work in the browser, Dozzle streams live.
