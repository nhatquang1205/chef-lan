# Deploying the API to the VPS

GitHub Actions builds the image, pushes it to GHCR, then copies the compose file, the deploy script
and the generated schema SQL to the VPS and runs it. The VPS never needs this repo or the .NET SDK.
Design and reasoning: `docs/deploy-api/`.

```
https://<domain>/api/ -> nginx -> 127.0.0.1:5100 -> cheflan-api (container) -> postgres (container)
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
Web__PublicBaseUrl=https://<customer website domain>

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

Paste `deploy/nginx-api.conf` (both `location` blocks) into the chef-lan-web `server` block, then
`sudo nginx -t && sudo systemctl reload nginx`.

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
4. Bucket settings -> Lifecycle: **keep only the last version** of a file. Deleting a thumbnail on a
   versioned bucket only hides it; without that rule B2 keeps (and bills) the hidden copy.
5. Images are small, so uploads go through the API (nginx allows 6 MB on `/api/`).

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

1. `curl -s https://<domain>/api/v1/courses` returns JSON; `curl -I https://<domain>/health` returns
   the website's 404 (the API's health endpoint is not public).
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
| Logs | `docker compose -f ~/chef-lan/docker-compose.prod.yml logs -f api` |
| Roll back | `cd ~/chef-lan && IMAGE_TAG=$(cat .last-good-tag) bash deploy-api.sh` (database changes are forward-only: keep migrations backwards-compatible for one release) |
| Backup | none yet - set up a nightly `docker exec postgres pg_dump -U cheflan cheflan` before real customers arrive |
