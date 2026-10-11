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
# Image uploads (Bunny Storage zone + its CDN hostname) - see "Bunny setup" below
Storage__Bunny__StorageZone=<zone name>
Storage__Bunny__AccessKey=<zone password>
Storage__Bunny__Endpoint=https://storage.bunnycdn.com
Storage__Bunny__CdnBaseUrl=https://<pull-zone>.b-cdn.net

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

## Bunny setup (for uploads)

Two separate Bunny products are used:

| What | Bunny product | Env vars |
|---|---|---|
| Videos | **Stream** library | `Video__Bunny__LibraryId`, `Video__Bunny__ApiKey` (library API key), `Video__Bunny__TokenSecurityKey` (playback signing) |
| Images | **Storage** zone + a **Pull zone** (CDN) in front of it | `Storage__Bunny__StorageZone`, `Storage__Bunny__AccessKey` (the zone's password), `Storage__Bunny__Endpoint` (must match the zone's main region), `Storage__Bunny__CdnBaseUrl` |

1. Dashboard -> Storage -> **Add Storage Zone** (note the name and region). Under it, **FTP & API
   Access** shows the zone **password**: that is `Storage__Bunny__AccessKey`.
2. Connect a **Pull Zone** to the storage zone (the zone's "Connected pull zone"); its hostname,
   `https://<name>.b-cdn.net`, is `Storage__Bunny__CdnBaseUrl`.
3. Regional endpoints: Falkenstein `https://storage.bunnycdn.com` (default), Los Angeles
   `https://la.storage.bunnycdn.com`, Singapore `https://sg.storage.bunnycdn.com`,
   Sydney `https://syd.storage.bunnycdn.com`.
4. Restart the API after editing `.env`. Without these variables the upload endpoints answer
   **503** (not configured); everything else keeps working.

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
