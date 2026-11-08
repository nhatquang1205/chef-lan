# Plan 07: move to bonbocon.com (courses / admin / api)

Why: the site runs on DuckDNS names today (`course-cheflan.duckdns.org`, `admin-cheflan.duckdns.org`, the API under `/api/`
of each). Chef Lan bought `bonbocon.com` (Cloudflare). The API gets its own host and both front ends call it cross-origin.

## Decisions (confirmed)

| | |
|---|---|
| Hosts | `courses.bonbocon.com` (website), `admin.bonbocon.com` (admin), `api.bonbocon.com` (API, routes keep `/api/v1/...`) |
| Admin | **Option B**: calls `https://api.bonbocon.com` directly (`VITE_API_BASE_URL`, from the GitHub variable `API_BASE_URL`); no `/api/` on the admin host |
| Website | Browser calls `https://api.bonbocon.com`; the Next server keeps calling the API internally (`API_BASE_URL=http://127.0.0.1:5100`) |
| CORS | Required now: `Cors__AllowedOrigins__0=https://courses.bonbocon.com`, `__1=https://admin.bonbocon.com` |
| DNS | Cloudflare, A records to the VPS (171.244.60.174), **proxied** (orange cloud), SSL/TLS **Full (strict)**, no AAAA. Created by the user. Certificates are issued by certbot first, then the proxy is switched on |
| Real IP | `deploy/nginx/cloudflare-realip.conf` in `conf.d`: `CF-Connecting-IP` trusted only from Cloudflare's ranges, so the login rate limit sees visitors |
| Video uploads | Cloudflare Free caps bodies at 100 MB / ~100 s: big videos are uploaded in the Bunny dashboard for now (no direct-to-Bunny upload in this plan) |
| Old DuckDNS hosts | **Switched off**: their site files are removed from `sites-enabled`. Old invite links stop working (resend them) |
| `bonbocon.com`, `www` | 301 to `https://courses.bonbocon.com` |
| TLS | Let's Encrypt via certbot on the VPS, as today |
| sudo | The VPS needs a password for sudo: nginx and certbot steps are run by the user; Claude prepares the files and runs the non-sudo steps over ssh |

## Repo changes

- `chef-lan/deploy/nginx/`: one file per server block, ready to copy to `/etc/nginx/sites-available/`:
  `api.bonbocon.com` (whole host proxied to 5100, `client_max_body_size 6m`, the uploads block unchanged), `bonbocon-root`
  (bonbocon.com + www → courses), `cloudflare-realip.conf`. Replaces the "add inside the website's server block" `deploy/nginx-api.conf`.
- `chef-lan-web/deploy/nginx.conf`: `server_name courses.bonbocon.com`, no `/api/` block.
- `chef-lan-admin/deploy/nginx.conf`: `server_name admin.bonbocon.com`, no `/api/` blocks.
- `chef-lan-admin/.github/workflows/deploy.yml`: `VITE_API_BASE_URL: ${{ vars.API_BASE_URL }}` (empty = same origin, so the
  change is safe to push before the variable exists).
- Docs: the three `deploy/README.md`, `chef-lan/CLAUDE.md` "Deployment" and "API conventions" (CORS no longer empty in
  production; API on its own host).

## VPS steps, in order

1. **User, Cloudflare:** A records `courses`, `admin`, `api`, `@`, `www` → 171.244.60.174, DNS only until certbot has run (step 3), TTL Auto.
2. **Claude:** copy the new nginx files to `~/nginx-bonbocon/` on the VPS (no sudo).
3. **User, sudo:** install the api, courses and admin server blocks (new hosts alongside the old ones), `nginx -t`, reload,
   `certbot --nginx -d api.bonbocon.com -d courses.bonbocon.com -d admin.bonbocon.com -d bonbocon.com -d www.bonbocon.com`
   (or one per host).
4. **Claude:** `~/chef-lan/.env`: `Web__PublicBaseUrl=https://courses.bonbocon.com`, the two CORS origins; restart the API with
   the current image. `~/chef-lan-web/.env.production`: `API_BASE_URL=http://127.0.0.1:5100`,
   `NEXT_PUBLIC_API_BASE_URL=https://api.bonbocon.com`, `NEXT_PUBLIC_SITE_URL=https://courses.bonbocon.com`; run the web
   deploy script (rebuild).
5. **User, GitHub:** admin repo variables `API_BASE_URL=https://api.bonbocon.com`, `ADMIN_URL=https://admin.bonbocon.com`,
   `PUBLIC_SITE_URL=https://courses.bonbocon.com`; web repo secret `APP_URL=https://courses.bonbocon.com`; re-run the admin
   workflow (or push).
6. **User, dashboards:** SePay IPN URL → `https://api.bonbocon.com/api/v1/payments/sepay/ipn`; Bunny Stream allowed referrers
   (if set) → add `courses.bonbocon.com`.
7. **User, sudo + Cloudflare:** install `cloudflare-realip.conf`, remove the old `chef-lan-web` / `chef-lan-admin` links from
   `sites-enabled` (DuckDNS off), `nginx -t`, reload; set SSL/TLS to Full (strict); switch every record to proxied.
8. **Claude:** verify (below).

## Verify

`curl -I` each host (200 / 301 as expected, valid certificate); `https://api.bonbocon.com/api/v1/checkout/config` answers;
a CORS preflight from each origin gets `Access-Control-Allow-Origin`; in the browser: website login, course page, a playback,
the API log shows visitors' IPs, not Cloudflare's; admin login, a list page, an image upload;
the sitemap lists `https://courses.bonbocon.com/...`; Bunny embeds play on the new host.
