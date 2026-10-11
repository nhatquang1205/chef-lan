# Plan 3 — Staff auth

**Status: done**, including `change-password` (added afterwards — see the open item below,
now resolved: 5 endpoints). It revokes *all* refresh tokens including the caller's, because the
request carries no refresh token to spare; wrong current passwords count toward lockout;
`auth-login` is attached. Request records are named `StaffRefreshRequest`
and `StaffLogoutRequest`; logout also takes the refresh token in its body. Added
`ValidationFilter` (400 `ValidationProblemDetails`), which course-content plan 3 also needs.

Depends on [plan 1](01-current-user.md) and [plan 2](02-rate-limiting.md).
See [spec.md](../spec.md) for the shared rotation, lockout and enumeration rules.

**Goal:** 5 endpoints. **This is the plan that unblocks `docs/course-content/` plans 3–6.**

---

## Endpoints

```
POST /api/v1/admins/auth/login    -> 200 StaffAuthResponse     anonymous   [auth-login]
POST /api/v1/admins/auth/refresh  -> 200 StaffAuthResponse     anonymous   [auth-refresh]
POST /api/v1/admins/auth/logout   -> 204                       [Internal]
GET  /api/v1/admins/me            -> 200 StaffResponse         [Internal]
```

There is **no staff registration endpoint, ever.** Staff accounts are created by an Admin
through an admin endpoint (a later feature); until then the seeded Admin is the only account.

```jsonc
// StaffAuthResponse
{
  "accessToken": "...",
  "accessTokenExpiresAt": "2026-10-07T12:15:00Z",
  "refreshToken": "...",          // raw, returned once
  "user": {
    "id": 1, "email": "admin@cheflan.local", "displayName": "Chef Lan",
    "role": "Admin", "jobTitle": "Owner",
    "mustChangePassword": true
  }
}
```

`login` and `refresh` are `[AllowAnonymous]` but still rate limited. `logout` and `me` use
the `Internal` scheme and reject a customer token.

## Behaviour

- **login** — find by email (`citext`, so case-insensitive), verify with BCrypt, check
  `is_active` and `locked_until`. On success: reset `failed_login_count`, clear
  `locked_until`, set `last_login_at`, issue the pair. On failure: increment, lock at 5.
- **refresh** — look up by `token_hash` only. Rotate: revoke the old, set
  `replaced_by_token_hash`, issue a new pair. Re-check `is_active` — a staff member
  offboarded five minutes ago must not be able to refresh.
- **reuse detection** — a presented token that is already revoked *and* has a
  `replaced_by_token_hash` means the chain was replayed. Revoke **every** token for that user
  and return 401.
- **logout** — revoke the presented refresh token. Return 204 even if it was already revoked;
  logout is idempotent and must not become an oracle.
- **enumeration** — unknown email and wrong password return the identical error and take the
  same time (BCrypt-verify a dummy hash when the user is not found).

## Resolved — `must_change_password`

The seeded Admin has `must_change_password = true` and the chosen endpoint set has no way to
clear it, so the flag is decoration and *enforcing* it would lock out the only admin.
[spec.md](../spec.md) recommends adding one endpoint here:

```
POST /api/v1/admins/auth/change-password   { currentPassword, newPassword }   [Internal]
```

No email, no token table, no migration. **Decide before starting.** If it is in, it also
clears the flag and revokes all other refresh tokens for that user (a password change should
end other sessions).

## Files

- `ChefLan.Logic/Identity/Interfaces/` — `IStaffAuthService`; extend `IUserRepository` and
  `IUserRefreshTokenRepository`
- `ChefLan.Logic/Identity/Services/StaffAuthService.cs` — `internal sealed`, registered by
  `IdentityModule`
- `ChefLan.Logic/Identity/Contracts/` — `StaffLoginRequest`, `RefreshRequest`,
  `StaffAuthResponse`, `StaffResponse`
- `ChefLan.Logic/Identity/Validators/`
- `ChefLan.Infrastructure/Repositories/Identity/` — implement both stubs (they currently
  throw `NotImplementedException`)
- `ChefLan.Courses.API/Controllers/Admin/AdminAuthController.cs`,
  `Controllers/Admin/AdminMeController.cs`

Reuse from `ChefLan.Commons/Auth/`: `IJwtTokenService.CreateStaffToken`, `IPasswordHasher`,
`RefreshTokenGenerator`, `IClock`. None of it needs changing.

## Done when

- seeded Admin logs in → access + refresh token, `mustChangePassword: true`
- `GET /api/v1/admins/me` with that token → 200 with role `Admin`
- `ADMIN@CHEFLAN.LOCAL` logs in the same account as `admin@cheflan.local` (citext)
- wrong password → 401; unknown email → **401 with the identical body**, and response times
  are within noise of each other
- 5 wrong passwords → 6th attempt **403 locked**; the *correct* password also 403 while
  locked; a successful login after expiry resets the counter
- `is_active = false` → 401/403 on login **and** on refresh
- refresh → new pair, old token revoked, `replaced_by_token_hash` set
- **replay the old refresh token → 401, and every token for that user is now revoked**
- logout → 204; the refresh token no longer works; a second logout → 204, not 404
- a **customer** token on `/api/v1/admins/me` → 401
- 6 logins in a minute → 429 with `Retry-After` (`auth-login` attached)
- after this plan, a staff token makes `docs/course-content/` plans 3–6 testable
