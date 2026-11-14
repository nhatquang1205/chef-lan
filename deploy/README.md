# Deploying the API to the VPS

GitHub Actions builds the image, pushes it to GHCR, then copies the compose file, the deploy script
and the generated schema SQL to the VPS and runs it. The VPS never needs this repo or the .NET SDK.
Design and reasoning: `docs/deploy-api/`.

```
https://api.bonbocon.com -> nginx -> 127.0.0.1:5100 -> cheflan-api (container) -> postgres (container)
```

## One-time setup on the VPS

Replace `<...>`. Run as the deploy user (the same one the web repo's workflow uses).

### 1. A database and login for chef-lan (never wish-list's)

Find the Postgres superuser: `docker exec postgres env | grep POSTGRES_USER`.

```bash
docker exec -i postgres psql -U <superuser> -d postgres <<'SQL'
CREATE ROLE cheflan LOGIN PASSWORD '<generate: openssl rand -base64 24>' NOSUPERUSER;
CREATE DATABASE cheflan OWNER cheflan ENCODING 'UTF8';
SQL
```

The schema script runs `CREATE EXTENSION IF NOT EXISTS citext`; on Postgres 13+ that is a *trusted*
extension, so the database owner may create it without superuser (checked on 17, as a non-superuser
owner). If the first deploy ever fails on that line, run `CREATE EXTENSION citext;` once inside the
`cheflan` database as the superuser.

### 2. The `.env` file

Create `~/chef-lan/.env` (mode 600) with:

```
DB_PASSWORD=<the cheflan role's password>
Jwt__Internal__SigningKey=<openssl rand -base64 48>
Jwt__Customer__SigningKey=<a DIFFERENT openssl rand -base64 48>
ForwardedHeaders__KnownProxies__0=172.30.10.1
Video__Bunny__LibraryId=<...>
Video__Bunny__ApiKey=<...>
Video__Bunny__TokenSecurityKey=<...>
# Online payment, SePay Payment Gateway (leave all of these out to keep checkout off):
Payment__BankTransferEnabled=true   # default false: customers contact chef Lan; the IPN still works when SePay is configured
Payment__SePay__MerchantId=<"Mã đơn vị" on SePay's merchant information page>
Payment__SePay__SecretKey=<the merchant Secret Key from that page: signs the checkout form>
Payment__SePay__IpnSecret=<the secret set on SePay's IPN tab: SePay sends it as X-Secret-Key>
# Image uploads: Backblaze B2 (S3-compatible, like wish-list) - see "Storage setup" below
Storage__B2__Bucket=<bucket name>
Storage__B2__Endpoint=https://s3.<region>.backblazeb2.com
Storage__B2__KeyId=<application key id>
Storage__B2__AppKey=<application key>
# Email (customer invitations): Gmail with an app password - see "Email setup" below
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=<the gmail address>
Smtp__Password=<16-character app password, no spaces>
Smtp__FromAddress=<the same gmail address>
Smtp__FromDisplayName=Bon Bocon
# The customer website (no trailing slash): invitation links point here. Required when Smtp__Host is set.
Web__PublicBaseUrl=https://courses.bonbocon.com
# The website and the admin call this API from the browser (cross-origin): both origins are required.
Cors__AllowedOrigins__0=https://courses.bonbocon.com
Cors__AllowedOrigins__1=https://admin.bonbocon.com
# A Discord message for each new contact order (same webhook URL as ~/chef-lan-monitoring/.env). Empty = only logged.
Notifications__Discord__WebhookUrl=<discord webhook url>
Notifications__Discord__AdminBaseUrl=https://admin.bonbocon.com

# FIRST DEPLOY ONLY - remove these three lines afterwards, then redeploy:
Seed__RunOnStartup=true
Seed__AdminEmail=<your admin email>
Seed__AdminPassword=<a strong temporary password>
```

- The two JWT keys **must differ** and must be new - never the development ones.
- `KnownProxies` must be the gateway of the compose network (`172.30.10.1` by default). If
  `docker network ls` already shows an overlapping subnet, set `API_SUBNET` and `API_GATEWAY` in
  `.env` and use the matching gateway here.
- Overridable in `.env`: `PG_HOST` (default `postgres`), `PG_NETWORK` (default `postgres_default`),
  `DB_NAME`, `DB_USER`, `API_PORT` (5100).

### 3. nginx

The API has its own host, `api.bonbocon.com`. In Cloudflare, the `A` records (`api`, `courses`, `admin`, `@`, `www`) point
at the VPS IP, **proxied** (orange cloud; no AAAA, the VPS has no IPv6), SSL/TLS mode **Full (strict)**. `deploy/nginx/` has one file per server block:

- `api.bonbocon.com`: copy to `/etc/nginx/sites-available/`, `sudo ln -s` into `sites-enabled`,
  `sudo nginx -t && sudo systemctl reload nginx`, then `sudo certbot --nginx -d api.bonbocon.com`. It proxies the
  whole host to 5100 (6 MB bodies, the uploads block with `proxy_request_buffering off`) and keeps `/health` unexposed.
- `bonbocon-root`: `bonbocon.com` and `www` -> `https://courses.bonbocon.com` (installed with the new hosts).
- `cloudflare-realip.conf`: goes to `/etc/nginx/conf.d/`; restores the visitor's IP from `CF-Connecting-IP` for requests from
  Cloudflare's ranges, so the API's per-IP login rate limit still sees real visitors.

Behind Cloudflare's proxy (Free plan) a request body is limited to **100 MB** and a response must start within ~100 s: a
big video cannot be uploaded through the admin. Upload it in the Bunny dashboard and paste its video ID into the lesson
video's `externalId`. The old DuckDNS hosts are switched off (their site files removed from `sites-enabled`).

The website and admin blocks are in their own repos (`chef-lan-web/deploy/nginx.conf`, `chef-lan-admin/deploy/nginx.conf`).
Every bonbocon.com server block starts with `include snippets/cloudflare-only.conf;` (see "Cloudflare-only origin" below):
a new block must too.
**SePay's IPN URL** (SePay dashboard) is `https://api.bonbocon.com/api/v1/payments/sepay/ipn`.

### Cloudflare-only origin

The bonbocon.com hosts answer only requests that come through Cloudflare, and a request to the bare IP or an unknown host gets
no answer (`444`). wish-list (`ninawishes.duckdns.org`, not behind Cloudflare) is untouched. Design:
`docs/deploy-api/plans/09-cloudflare-only-origin.md`. Files:

- `cloudflare-only.conf` -> `/etc/nginx/conf.d/`: sets `$from_cloudflare` from `$realip_remote_addr`, the real TCP peer.
  (`allow`/`deny` would not work: `cloudflare-realip.conf` has already turned `$remote_addr` into the visitor's address.)
- `snippets/cloudflare-only.conf` -> `/etc/nginx/snippets/`: `return 444` unless `$from_cloudflare`. Included at the top of
  every bonbocon server block, port 80 and 443.
- `default-deny` -> `sites-available` + `sites-enabled`: the default server. nginx 1.18 has no `ssl_reject_handshake`, so it
  needs a self-signed certificate.
- Cloudflare's ranges are in **two files** (`cloudflare-realip.conf` and `cloudflare-only.conf`): keep them identical.

From your machine:

```bash
scp deploy/nginx/cloudflare-only.conf deploy/nginx/default-deny nina:nginx-bonbocon/
scp deploy/nginx/snippets/cloudflare-only.conf nina:nginx-bonbocon/cloudflare-only.snippet
```

On the VPS (`sed` edits the files in `sites-available`, never the symlinks in `sites-enabled`, and leaves a `.bak-cfonly` copy
next to each; nginx does not load `sites-available`):

```bash
cd ~/nginx-bonbocon
sudo mkdir -p /etc/nginx/ssl
sudo openssl req -x509 -newkey rsa:2048 -nodes -days 3650 -subj /CN=invalid -keyout /etc/nginx/ssl/default-deny.key -out /etc/nginx/ssl/default-deny.crt
sudo chmod 600 /etc/nginx/ssl/default-deny.key
sudo cp cloudflare-only.conf /etc/nginx/conf.d/cloudflare-only.conf
sudo cp cloudflare-only.snippet /etc/nginx/snippets/cloudflare-only.conf
sudo cp default-deny /etc/nginx/sites-available/default-deny
sudo ln -s /etc/nginx/sites-available/default-deny /etc/nginx/sites-enabled/default-deny
cd /etc/nginx/sites-available
for f in admin.bonbocon.com api.bonbocon.com bonbocon-root courses.bonbocon.com monitor.bonbocon.com; do sudo sed -i.bak-cfonly '/^server {$/a\    include snippets/cloudflare-only.conf;' "$f"; done
grep -c 'include snippets/cloudflare-only.conf' admin.bonbocon.com api.bonbocon.com bonbocon-root courses.bonbocon.com monitor.bonbocon.com   # 2 each
sudo nginx -t && sudo systemctl reload nginx
```

Check:

```bash
curl -sI https://courses.bonbocon.com/ | head -1                                          # 200, through Cloudflare
curl -sk --resolve courses.bonbocon.com:443:127.0.0.1 https://courses.bonbocon.com/ ; echo $?   # 52: empty reply (direct hit)
curl -sk https://171.244.60.174/ ; echo $?                                                  # 52
curl -sI https://ninawishes.duckdns.org/ | head -1                                         # wish-list still answers
sudo certbot renew --dry-run                                                                # every name still renews
```

Roll back: in `/etc/nginx/sites-available`, `for f in admin.bonbocon.com api.bonbocon.com bonbocon-root courses.bonbocon.com monitor.bonbocon.com; do sudo mv "$f.bak-cfonly" "$f"; done`, `sudo rm /etc/nginx/sites-enabled/default-deny`, `sudo nginx -t &&
sudo systemctl reload nginx`.

**ufw.** Only 22, 80 and 443 are public today (every Docker port is bound to `127.0.0.1`, which is what protects them: Docker's
rules bypass ufw). Keep the current SSH session open until a new one connects:

```bash
sudo ufw status verbose            # see what is there first
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status verbose
```

Then, from a second terminal, `ssh nina` must still work, and the sites, Dozzle and Netdata too. Undo: `sudo ufw disable`.

### 4. GitHub

Environment `production` with secrets `SSH_PRIVATE_KEY`, `VPS_HOST`, `VPS_USER` (and `VPS_PORT` if
not 22) - **per repository**, so add them to this repo too. After the first push to `main` builds the
image, make the package public once (GitHub -> your profile -> Packages -> `chef-lan-api` -> Package
settings -> Change visibility). The image holds no secrets, and then the VPS pulls it without a login.

## Storage setup (for uploads)

Two services, split by what they are good at (the same split as wish-list for images):

| What | Service | Env vars |
|---|---|---|
| **Images** (thumbnails) | **Backblaze B2**, S3-compatible API, a **public** bucket | `Storage__B2__Bucket`, `Storage__B2__Endpoint`, `Storage__B2__KeyId`, `Storage__B2__AppKey`; optional `Storage__B2__Region`, `Storage__B2__PublicBaseUrl` |
| **Videos** | **Bunny Stream** library | `Video__Bunny__LibraryId`, `Video__Bunny__ApiKey` (library API key), `Video__Bunny__TokenSecurityKey` (playback signing) |

**B2 (images)**
1. B2 -> Buckets -> **Create a Bucket**: files **Public**, a new bucket just for chef-lan (not wish-list's).
   Note the **Endpoint** shown on the bucket (`s3.<region>.backblazeb2.com`).
2. App Keys -> **Add a New Application Key**: restrict it to **that bucket only**, read and write. B2 shows
   the **keyID** and the **applicationKey** once: `Storage__B2__KeyId` and `Storage__B2__AppKey`.
3. Images are served from `https://<bucket>.s3.<region>.backblazeb2.com/images/...` unless you set
   `Storage__B2__PublicBaseUrl` (a custom domain or CDN in front of the bucket, no trailing slash).
   Production uses `https://img.bonbocon.com` (Cloudflare in front of the bucket); setup and the one-time URL rewrite
   (`deploy/sql/rewrite-image-base-url.sql`) are in `docs/deploy-api/plans/10-image-cdn.md`.
4. Bucket settings -> Lifecycle: **keep only the last version** of a file. Deleting a thumbnail on a
   versioned bucket only hides it; without that rule B2 keeps (and bills) the hidden copy.
5. Images are small, so uploads go through the API (nginx allows 6 MB on the API host).

**Bunny (videos)**: library id and the two keys from the Stream library's dashboard.

**Email setup (Gmail)**
1. Use the Google account that should send the mails (its address is the sender). Google Account -> Security ->
   **2-Step Verification** must be on.
2. Google Account -> Security -> **App passwords** (or https://myaccount.google.com/apppasswords): create one named
   "Bon Bocon API". Google shows 16 letters once: that is `Smtp__Password` (write it without spaces).
3. `Smtp__FromAddress` must be that same Gmail address; Gmail rewrites any other sender anyway.
4. Gmail allows roughly 500 mails a day from one account, plenty for invitations. If a mail lands in spam, mark it
   "not spam" once from the receiving account.

Without `Smtp__Host` the API does not send mail at all: each mail is only logged with a warning, and invitations
are never delivered.

Restart the API after editing `.env`. Without the B2 values the image upload endpoints and the
file form of course create/update answer **503** (not configured); without the Bunny values the video
upload answers 503. Everything else keeps working.

## After the first deploy

1. `curl -s https://api.bonbocon.com/api/v1/courses` returns JSON; `curl -I https://api.bonbocon.com/health` returns
   404 (the API's health endpoint is not public).
2. Log in as the seeded admin (`POST /api/v1/admins/auth/login`), call
   `POST /api/v1/admins/auth/change-password`, then **delete the `Seed__*` lines from `.env`** and redeploy.
3. **Check the rate limit sees real clients:** six wrong logins from your own connection must give a
   429 on the 6th, and a login from another network (a phone on mobile data) must still be allowed.
   If both are blocked together, `KnownProxies` does not match the gateway the API actually sees:
   look at `docker compose -f docker-compose.prod.yml logs api` and `docker network inspect cheflan_cheflan`.
4. Point the website at it: `docs/deploy-api/plans/06-web-switch-over.md`.

## Operations

| | |
|---|---|
| Logs | Dozzle (see Monitoring), or `docker compose -f ~/chef-lan/docker-compose.prod.yml logs -f api` |
| Roll back | `cd ~/chef-lan && IMAGE_TAG=$(cat .last-good-tag) bash deploy-api.sh` (database changes are forward-only: keep migrations backwards-compatible for one release) |
| Old images | every successful deploy removes API images except the new one and the previous one |
| Backup | nightly 03:00, encrypted `pg_dump` of `cheflan` to a private B2 bucket, a Discord line each morning (see Backup) |

## Backup

`deploy/backup/backup-db.sh` runs from the deploy user's crontab every night at 03:00 (VPS clock `Asia/Ho_Chi_Minh`): `pg_dump -Fc`
of the `cheflan` database only (not wish-list) from the shared `postgres` container, checked with `pg_restore --list`, encrypted with
gpg (AES256), uploaded to a private Backblaze B2 bucket with rclone in Docker (pinned image, nothing to install). Discord gets one
line on success (`Backup cheflan OK: 2.1 MB, 14 bản trên B2`) and one on failure (step and last error line): **no line by the
morning means the job did not run**. Design: `docs/deploy-api/plans/08-database-backup.md`.

### Setup

1. B2: create a **private** bucket just for backups (not the public image bucket). Lifecycle rule: hide files after 14 days, delete
   1 day after hiding. That is what enforces retention, so the VPS key needs no delete right.
2. B2: create an application key restricted to that bucket. The script only needs to list and write files; give it the narrowest
   capabilities B2 offers (`listBuckets`, `listFiles`, `readFiles`, `writeFiles`, no `deleteFiles`). B2's "Read and Write" preset
   includes delete, so use custom capabilities if the dashboard offers them. Uploading a new, uniquely named file should not need
   `deleteFiles`; this is not tested against B2 (see the plan's notes), so if the first run fails with an authorization error,
   add the missing capability and keep the lifecycle rule anyway.
3. Generate the passphrase and put it in the password manager **before anything else**: `openssl rand -base64 32`. Lose it and
   every backup is unreadable.
4. Copy the folder and fill in the secrets:

```bash
ssh nina 'mkdir -p ~/chef-lan-backup'
scp deploy/backup/backup-db.sh deploy/backup/.env.example deploy/backup/crontab.txt nina:chef-lan-backup/
ssh nina
cd ~/chef-lan-backup
chmod 700 backup-db.sh
cp .env.example .env && chmod 600 .env && nano .env      # webhook URL, passphrase, B2 key, bucket
./backup-db.sh                                           # run once by hand: expect a green line in Discord
crontab -e                                               # paste the line from crontab.txt
```

`flock` and `gpg` come with Ubuntu. `jq` is not needed (the Discord JSON is escaped in bash). The log is
`~/chef-lan-backup/backup.log`; the script keeps its last 1000 lines. To check the schedule: `crontab -l`.

### Restore

Never restore straight over production: restore into a scratch database, look at it, then decide.

```bash
# 1. download the newest file (B2 web UI, or rclone from any machine that has the B2 key) into one folder, then:
gpg --output cheflan.dump --decrypt cheflan-YYYYMMDD-HHMMSS.dump.gpg      # asks for the passphrase
# 2. restore into a scratch database on the shared Postgres
docker exec postgres createdb -U cheflan cheflan_restore
docker exec -i postgres pg_restore -U cheflan -d cheflan_restore --no-owner < cheflan.dump
docker exec postgres psql -U cheflan -d cheflan_restore -c 'select count(*) from customers.customers'
# 3. when done: docker exec postgres dropdb -U cheflan cheflan_restore ; rm cheflan.dump
```

`docker exec postgres env | grep POSTGRES_USER` shows the superuser if it is not `cheflan`. A restore has not been rehearsed yet:
do it once on a scratch database before relying on it.

### Good to know

- A second run while one is still going is refused (a lock file) and reported to Discord as a failure.
- Changing the passphrase later does not touch old files: keep the old one until those files age out of the bucket.
- Only the database is backed up: not the VPS `.env` files, nor B2 or Bunny content.

## Monitoring

Dozzle (API logs, and a Discord message for each API error) and Netdata (CPU, RAM, disk,
per-container usage, nginx responses by status class). Both listen on `127.0.0.1` only. Design: `docs/monitoring/`.
It is a separate compose project (`cheflan-monitoring`, in `~/chef-lan-monitoring`), because `deploy-api.sh` removes
anything else in the `cheflan` project. The API needs the JSON log setting in `docker-compose.prod.yml`, so deploy the
API once first.

### Setup (no sudo: the deploy user is in group `docker`)

1. Discord: in the channel, Edit Channel -> Integrations -> Webhooks -> New Webhook -> Copy Webhook URL. It is a
   credential: it goes only in the `.env` below, never in git.
2. Check what Netdata needs from the host: `ls -l /var/log/nginx/access.log` should show group `adm`, and
   `getent group adm` should say gid `4`. If the gid differs, change `group_add` in `monitoring/docker-compose.yml`.
3. From your machine, copy the folder, then on the VPS fill in the secret and start it:

```bash
ssh nina 'mkdir -p ~/chef-lan-monitoring/dozzle-data'
scp -r deploy/monitoring/* deploy/monitoring/.env.example nina:chef-lan-monitoring/
ssh nina
cd ~/chef-lan-monitoring
cp .env.example .env && chmod 600 .env && nano .env      # paste the webhook URL
cp notifications.yml dozzle-data/notifications.yml
docker compose up -d
```

### Use

```bash
ssh -N -L 8081:127.0.0.1:8081 -L 19999:127.0.0.1:19999 nina
```

Dozzle is then at http://localhost:8081/logs/, Netdata at http://localhost:19999. Without a tunnel: https://monitor.bonbocon.com
(`/system/` Netdata, `/logs/` Dozzle, basic auth; `deploy/nginx/monitor.bonbocon.com`). Check the alert in Dozzle ->
Notifications -> Test.

### Updating

Edit the files in the repo, copy them again (`scp` as above; `cp notifications.yml dozzle-data/` and
`docker compose restart dozzle` if the alert changed), then `docker compose up -d`. To move to a new image version, change
the tag in `monitoring/docker-compose.yml`.

### Uptime check

`.github/workflows/uptime.yml` runs every 5 minutes (GitHub may start it 5 to 15 minutes late) from outside the VPS and checks
`courses.bonbocon.com/`, `admin.bonbocon.com/` (must contain `<div id="root">`) and `api.bonbocon.com/api/v1/health` (`/health`
is not public). A site counts as down after 3 failed tries 20 s apart. It posts to Discord only when a site goes down and when it
is back (with the downtime). Design: `docs/monitoring/plans/03-uptime-check.md`.

- One-time setup: repo Settings -> Secrets and variables -> Actions -> new repository secret `DISCORD_WEBHOOK_URL` (the same
  webhook as Dozzle), then Actions -> Uptime -> Run workflow once.
- The job succeeds even while a site is down; the up/down state lives in the `uptime-state` artifact (2 days).
- **GitHub disables scheduled workflows after 60 days without commits to the repo**: if the checks stop, Actions -> Uptime ->
  Enable workflow.
- Locally: `DRY_RUN=1 STATE_FILE=/tmp/uptime.json .github/scripts/uptime-check.sh` prints the message instead of posting.

### Good to know

- Dozzle's history is Docker's own log (about 30 MB) and a deploy replaces the container, so older lines are gone. Alerts are not affected.
- EF Core and the request logger are left out of the alert, because they repeat what the exception handler reports: one failing
  request is one message. Dozzle has no cooldown for log alerts, and Discord rate-limits a webhook, so a burst of errors loses some.
- The nginx access log is shared by every site on this server, so the status counts are not chef-lan's alone.
- Netdata's Postgres collector is off (`netdata/go.d.conf`), so it does not try to log in to the shared Postgres it can find through the Docker socket.
