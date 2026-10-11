# Authentication — spec

Two completely separate login flows: **staff** (internal) and **customers** (external). They
share a password hasher and a refresh-token generator and nothing else.

This feature is the **gate** for `docs/course-content/` plans 3–6: every admin endpoint there
is `[Authorize]`, and until staff login exists there is no way to obtain a token.

## Plans

| # | Plan | Endpoints | Status |
|---|---|---|---|
| 1 | [`ICurrentUser` middleware](plans/01-current-user.md) | — | **done** |
| 2 | [Rate limiting policies](plans/02-rate-limiting.md) | — | **done** |
| 3 | [Staff auth](plans/03-staff-auth.md) | 5 | **done** |
| 4 | [Customer auth](plans/04-customer-auth.md) | 5 | **done** |

**10 endpoints.** Plan 3 is what unblocks course-content; plan 4 can follow later.

> Plan 1 was originally `docs/course-content/plans/02-current-user.md`. It moved here because
> it is authentication infrastructure and auth now lands first. The course-content plan file
> is a stub pointing at this one.

---

## Context

`identity.users` is internal staff (`Admin`, `Staff`). `customers.customers` is external
buyers. They are **disjoint populations** — no foreign key either way, no shared credential,
no shared token. A staff member who buys a course registers a separate customer account.

Both tables already carry their own credentials and lockout columns, and both refresh-token
tables exist. Nothing in this feature needs a migration.

## Decisions

| | |
|---|---|
| Staff routes | `/api/v1/admins/auth/*`, `/api/v1/admins/me` |
| Customer routes | `/api/v1/auth/*`, `/api/v1/me` |
| Staff registration | **none** — an Admin creates staff accounts |
| Shared web login | **`/api/v1/auth/login`, `/refresh`, `/logout`, `/me` accept both populations** (added later, so the one web login form works for admins). Customer credentials first, then staff; separate tokens; see CLAUDE.md § Auth |
| Refresh token transport | **JSON response body**, not a cookie |
| Customer activation / invite | **out of scope** — later feature |
| Password reset | **out of scope** |
| Rate limiting | **in scope**, and set up before any login endpoint exists |

### Why the body, not a cookie

One code path for web and a future native app, and no CSRF surface on the most
security-sensitive endpoint. The cost is real and should be stated: the client stores the
token, and `localStorage` is readable by any XSS on the page. Rotation with reuse detection
is what limits the damage — a stolen token works once, and the moment the real client
refreshes, the theft is detected and the whole chain is revoked.

### Token lifetimes — already configured

| | access | refresh |
|---|---|---|
| staff (`Jwt:Internal`) | 15 min | 1 day |
| customer (`Jwt:Customer`) | 15 min | 30 days |

Staff sit at a desk, so re-login is cheap. Nobody wants to log in again to watch a video.

---

## Rules that apply to both flows

### Refresh-token rotation and reuse detection

- The raw token is returned **once**. Only its SHA-256 hash is stored, so a database leak
  does not hand out sessions. `RefreshTokenGenerator` already does this.
- Refresh **rotates**: revoke the presented token, set its `replaced_by_token_hash`, issue a
  new one.
- **Reuse detection:** if a presented token is already revoked *and* has a
  `replaced_by_token_hash`, the chain has been replayed — someone is using a stolen copy.
  Revoke **every** token for that account and return 401. This is the entire reason
  `replaced_by_token_hash` exists.
- Logout revokes the presented token. An access token cannot be revoked, so it stays valid
  until it expires — hence the 15-minute lifetime.

### Lockout

`failed_login_count` and `locked_until` already exist on both tables.

- 5 consecutive failures → `locked_until = now + 15 minutes`
- a successful login resets the count and clears the lock
- a locked account returns **403**, not 401, and the same way whether or not the password was
  correct

Lockout protects one account; rate limiting (plan 2) protects the endpoint. Both are needed.

### Do not leak whether an email exists

A wrong password and an unknown email must return the **same** error and take the **same
time**. When the account is not found, still run a BCrypt verify against a dummy hash —
otherwise the response time answers the question for an attacker.

### `is_active`

A deactivated staff user or customer cannot log in and cannot refresh. Offboarding must
**also revoke that account's refresh tokens**; flipping `is_active` alone leaves a valid
refresh token working until it expires.

---

## Open items — decide before plan 3 / 4

### 1. Does a not-yet-activated customer get a distinct error?

A staff-created customer has `password_hash = NULL` and cannot log in. Two choices:

- **"Not activated, ask for an invite"** — useful for chef Lan's customers, who will be
  confused. But it is an account-enumeration oracle: it confirms the email exists.
- **Generic invalid credentials** — safe, and leaves the customer stuck with no explanation.

The emails are semi-public anyway (they are Facebook group members), so the leak is mild.
Plan 4 assumes the **distinct 403**; say if you want generic.

### 2. The seeded admin can never clear `must_change_password` — resolved, `change-password` added

`CoursesDbSeeder` creates the first Admin with `must_change_password = true`, and the chosen
endpoint set has no change-password endpoint. So the flag can be returned but never cleared,
and enforcing it would lock the only admin out of the system.

Recommend adding **one** endpoint to plan 3:

```
POST /api/v1/admins/auth/change-password   { currentPassword, newPassword }
```

No email, no token table, no migration — it closes the hole for the cost of one handler.
Without it, `must_change_password` is decoration.

---

## Verification across the feature

After plan 4, with no prior tokens:

1. staff login with the seeded admin → access + refresh token, `mustChangePassword: true`
2. `GET /api/v1/admins/me` with that token → 200
3. **customer token on `/api/v1/admins/me` → 401** (wrong scheme, wrong signing key)
4. **staff token on `/api/v1/me` → 401** — the mirror case, equally important
5. refresh → new pair; **replay the old refresh token → 401 and the whole chain revoked**
6. 6 bad passwords → 6th returns 403 locked, and the correct password also returns 403 until
   the lock expires
7. 6 rapid logins from one IP → 429 with `Retry-After`
8. register a customer → login → `GET /api/v1/me` → 200
9. a staff-created customer with `password_hash NULL` → login 403, not 500

Steps 3, 4 and 5 deserve integration tests under `tests/` with `Testcontainers.PostgreSql`.
They are the security boundary of the whole product, and they are cheap to assert and
expensive to get wrong.
