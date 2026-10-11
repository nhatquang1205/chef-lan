# Plan 0 — Backend: CORS

**Status: done.** A change in *this* repo, not the web repo.

**Goal:** let the browser app call the API directly.

> Needed because session tokens live in the browser (localStorage) — see [plan 6](06-login.md).

- Named CORS policy `Web`, origins from configuration (`Cors:AllowedOrigins`, an array),
  **never** `AllowAnyOrigin`. Local: `http://localhost:3000`.
- Methods and headers: whatever the API uses (`Authorization`, `Content-Type`).
- No credentials: refresh tokens travel in the JSON body, not cookies. `Retry-After` is exposed so the login form can show the wait.
- `app.UseCors("Web")` before authentication.
- Production origins via environment variables (`Cors__AllowedOrigins__0`).

## Done when

- a preflight `OPTIONS` from `http://localhost:3000` to `/api/v1/auth/login` returns the
  allow headers; from any other origin it does not
- `appsettings.json` has an empty list, `appsettings.Development.json` has `localhost:3000`
