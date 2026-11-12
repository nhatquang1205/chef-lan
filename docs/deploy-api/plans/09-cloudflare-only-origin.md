# Plan 9: bonbocon.com hosts answer only Cloudflare; ufw on

Why: every bonbocon.com host is proxied by Cloudflare, but nginx still answers anyone who connects to the VPS IP directly,
which skips Cloudflare's protection (and its caps, cache and bot checks). A request to the bare IP is answered by whichever
site nginx picks first.

## Decisions (confirmed)

| | |
|---|---|
| Where | **In nginx, per site**, not in the firewall: the same nginx serves `ninawishes.duckdns.org` (wish-list), which is not behind Cloudflare and must keep working, including its certbot renewals |
| How | `$realip_remote_addr` (the TCP peer, **before** `cloudflare-realip.conf` rewrites `$remote_addr` to the visitor) checked against Cloudflare's ranges with a `geo` map; a bonbocon server block answers anything else with `444` (connection closed, no response). Using `allow`/`deny` would not work: they see the rewritten visitor address and would block everyone |
| Bare IP / unknown host | A `default_server` on 80 and 443 that returns `444`. nginx here is 1.18, which has no `ssl_reject_handshake`, so the 443 default needs a self-signed certificate |
| ufw | Enabled: deny incoming, allow `22/tcp`, `80/tcp`, `443/tcp`. Every Docker port is bound to `127.0.0.1` already (Docker rules bypass ufw, so that binding is what protects them) |
| Who | Claude prepares the files and tests them in a throwaway nginx 1.18 container with copies of the live site files; the user runs every sudo step |

## Build

1. `deploy/nginx/cloudflare-only.conf` (conf.d, http context): `geo $realip_remote_addr $from_cloudflare { default 0; <ranges> 1; }`
   with the same ranges as `cloudflare-realip.conf` (dated).
2. `deploy/nginx/snippets/cloudflare-only.conf`: `if ($from_cloudflare = 0) { return 444; }`, included in **every server block**
   of `admin.bonbocon.com`, `api.bonbocon.com`, `bonbocon-root`, `courses.bonbocon.com`, `monitor.bonbocon.com` (port 80 and 443
   blocks, certbot added some of them). The live files carry certbot's edits, so the include is inserted on the VPS with a
   `sed` after each `server_name … bonbocon.com …;` line, with a backup of each file first. The repo copies of the site files
   get the same line.
3. `deploy/nginx/default-deny`: `listen 80 default_server; listen [::]:80 default_server; listen 443 ssl default_server;
   listen [::]:443 ssl default_server; server_name _; ssl_certificate` the self-signed pair; `return 444;`. Check it does not clash
   with the `ipv6only=on` listen certbot wrote.
4. Steps for the user (in `deploy/README.md`, "Cloudflare-only origin"): self-signed cert with `openssl req -x509 … -subj
   /CN=invalid`, copy the files, `sed` the include into the five live files, `sudo nginx -t`, reload, then verify: the sites work
   through Cloudflare; `curl -k --resolve courses.bonbocon.com:443:171.244.60.174 https://courses.bonbocon.com/` gets an empty
   reply; `https://171.244.60.174/` gets an empty reply; wish-list still works; `sudo certbot renew --dry-run` passes for every
   name. Then ufw (allow first, enable last, keep the SSH session open while checking a new one connects). Rollback: restore the
   backed-up files, `sudo ufw disable`.
5. Note in CLAUDE.md "Deployment": a new bonbocon server block must include the snippet; Cloudflare ranges are listed in two
   files now.

## Done when

`nginx -t` passes in an `nginx:1.18` container with the live site files (cert paths swapped for test certs); in that container a
request from a non-Cloudflare address to a bonbocon host gets no response, the wish-list host still answers, and the default server
returns 444. The user has run the VPS steps.

## Implementation notes

- Files: `deploy/nginx/cloudflare-only.conf` (geo, 22 ranges, identical to `cloudflare-realip.conf`, checked against Cloudflare's
  lists on 2026-11-12), `deploy/nginx/snippets/cloudflare-only.conf`, `deploy/nginx/default-deny`. The include goes right after each
  `server {` line, before certbot's `if ($host …) return 301`, so a direct hit on port 80 gets 444 rather than a redirect. The repo
  copies (`api.bonbocon.com`, `bonbocon-root`, `monitor.bonbocon.com`, and the web and admin repos' `deploy/nginx.conf`) carry it too.
- The `sed` must run on `sites-available`: GNU `sed -i` on a symlink in `sites-enabled` replaces the link with a plain file, and a
  `.bak` file left in `sites-enabled` would be loaded as a second copy of every server block.
- Tested in `nginx:1.18` with copies of the six live site files (certbot paths swapped for a test cert), running the exact `sed`
  loop from the README: `nginx -t` passes, each bonbocon file has the include twice; from a non-Cloudflare address every bonbocon
  host on 80 and 443, the bare IP and an unknown host get an empty reply (curl 52); wish-list still answers (301 on 80, 502 on 443
  because nothing ran behind it); with the test network added to the map, admin and courses are served again.
- Not tested: the real VPS, certbot renewal through Cloudflare, ufw.
