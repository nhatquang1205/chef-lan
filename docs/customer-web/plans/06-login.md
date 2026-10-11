# Plan 6 — Login

**Status: done**, with **localStorage** (the alternative below). A `/register` page was added too. "My courses" is out of scope.

**Goal:** a customer can sign in at `/login`, see that they are signed in, and sign out.

## Backend contract (already built)

`POST /api/v1/auth/login {email, password}` -> `{accessToken, accessTokenExpiresAt,
refreshToken, customer{id,email,fullName,phone}}`. `POST /auth/refresh {refreshToken}` rotates
both (a replayed refresh token revokes the whole session). `POST /auth/logout {refreshToken}`.
`GET /me`. Access token 15 min, refresh token 30 days.

Error mapping the form must handle:

| Backend | Meaning | Message |
|---|---|---|
| 400 | blank/invalid field | field errors |
| 401 `customer.invalid_credentials` | wrong email or password | generic "email or password is incorrect" |
| 403 `customer.not_activated` | recorded by staff, never set a password | explain they have no password yet, contact the school |
| 403 `customer.account_locked` | 5 failures | "temporarily locked, try again in 15 minutes" |
| 403 `customer.account_inactive` | disabled | contact the school |
| 429 | rate limited (5 logins/min) | "too many attempts" with `Retry-After` |

## Not chosen: server-side session (httpOnly cookies)

- A Next.js **route handler** (`/api/auth/login`, `/refresh`, `/logout`) calls the backend and
  stores the tokens in **httpOnly, Secure, SameSite=Lax cookies**. Browser JavaScript never
  sees a token, so an XSS bug cannot steal the session.
- Next.js middleware refreshes the access token when it is near expiry; a failed refresh clears
  the cookies and the user is signed out.
- No CORS needed ([plan 0](00-backend-cors.md) can be skipped). The backend sees the Next.js
  server's IP, not the user's, so the **IP-based rate limit becomes one shared bucket** unless
  the real client IP is forwarded and the backend trusts that proxy (`ForwardedHeaders:KnownProxies`).
  The per-email limit still works. This needs a backend config change at deploy time.

**Chosen:** the browser calls the API directly and stores the session in `localStorage`.
Simpler, needs CORS ([plan 0](00-backend-cors.md)), and any XSS can read the tokens; rotation
with reuse detection on the backend limits a stolen session to one use. Revisit if the site
ever handles anything more sensitive than course access.

## Page and header

- `/login`: email, password, show/hide password, submit with a loading state, error area
  (`role="alert"`), autocomplete attributes for password managers. Safe `?next=` redirect
  (same-origin paths only, to avoid an open redirect). After login: `next` or `/`.
- Signed in: the header's Login item becomes the customer's name with a menu (Logout). My
  courses is **not** added.
- Already signed in and visiting `/login` -> redirect to `/`.
- Logout calls the backend (so the refresh token is revoked) and clears the cookies.

## Done when

- valid credentials -> redirected, header shows the name; reload keeps the session
- each error row in the table above shows its message, in both locales
- 6 bad logins in a minute show the rate-limit message, not a crash
- logout revokes the token (a copied refresh token no longer works) and clears the cookies
- `?next=https://evil.example` is ignored
- no token is ever visible to page JavaScript (cookie flags checked in dev tools)
