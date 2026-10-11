# Plan 6 — Web repo switch-over

**Status: todo.** In `chef-lan-web`, after the API answers on `https://<domain>/api/`.

On the VPS, in `~/chef-lan-web/.env.production`:

```
API_BASE_URL=http://127.0.0.1:5100            # server-side calls: straight to the container, no TLS hop
NEXT_PUBLIC_API_BASE_URL=https://<domain>     # browser calls: same origin, so no CORS
```

Then re-run the web deploy: `NEXT_PUBLIC_API_BASE_URL` is baked in at build time.

Nothing else changes in the web repo. The backend's `Cors:AllowedOrigins` stays **empty** in
production (same origin needs none).

## Done when

- the landing page shows real categories and courses (not the "could not load data" text)
- register -> login -> reload keeps the session; logout revokes it, from the live site
- the browser's network tab shows `https://<domain>/api/v1/...` with no CORS preflight
