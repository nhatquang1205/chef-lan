# Plan 4 — Compose and nginx

**Status: done** (`deploy/docker-compose.prod.yml`, `deploy/nginx-api.conf`); not yet run on the VPS.

## docker-compose.prod.yml (in this repo; the VPS keeps only the `.env`)

- one service, `api`: the image from plan 2, `restart: unless-stopped`
- `ports: ["127.0.0.1:5100:8080"]` — loopback only; nginx and the web app's server-side calls use it
- joins the **external** network the Postgres container is on (`networks: [{name: <net>, external: true}]`)
- `env_file: .env` plus `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS=http://+:8080`
- log rotation: `logging: {driver: json-file, options: {max-size: "10m", max-file: "3"}}`
- no volumes (stateless; uploads are not stored locally yet)

`.env` on the VPS (never committed):

```
ConnectionStrings__ChefLanDb=Host=<pg-container>;Port=5432;Database=cheflan;Username=cheflan;Password=…
Jwt__Internal__SigningKey=<openssl rand -base64 48>
Jwt__Customer__SigningKey=<a DIFFERENT openssl rand -base64 48>
ForwardedHeaders__KnownProxies__0=<docker network gateway IP>
Video__Bunny__LibraryId=…
Video__Bunny__ApiKey=…
Video__Bunny__TokenSecurityKey=…
# first deploy only (plan 1): Seed__RunOnStartup=true  Seed__AdminEmail=…  Seed__AdminPassword=…
```

## nginx — add to the **chef-lan-web server block** (not wish-list's)

```nginx
location /api/ {
    proxy_pass http://127.0.0.1:5100;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $remote_addr;   # overwrite, don't append: clients can't forge it
    proxy_set_header X-Forwarded-Proto $scheme;
    client_max_body_size 1m;                          # the API takes small JSON only
}
```

`/api/` is matched before `/`, so the web app keeps everything else. `/health` is not proxied.

## Done when

- `curl https://<domain>/api/v1/courses` -> 200 JSON, and `curl https://<domain>/` is still the website
- `curl https://<domain>/health` and `/scalar` -> served by Next (404), not the API
- `docker compose logs api` shows the **client's** IP on a login attempt, not 172.x
- six logins from one IP -> 429; a login from a second IP at the same time is *not* limited
- wish-list on its own domain is unaffected
