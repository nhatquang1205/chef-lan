# Plan 4 — Customer auth

**Status: done.** The distinct 403 `not_activated` is implemented as the plan assumed. Request
records: `CustomerRefreshRequest`, `CustomerLogoutRequest`. Register saves twice (the token's
`sub` needs the generated id), so a failure between the two leaves an account that can log in
but was not auto-logged-in.

Depends on [plan 1](01-current-user.md) and [plan 2](02-rate-limiting.md).
Mirrors [plan 3](03-staff-auth.md) — read that first; only the differences are spelled out
here. See [spec.md](../spec.md) for the shared rules.

**Goal:** 5 endpoints. Not a blocker for the content CRUD plans, so it can follow plan 3.

---

## Endpoints

```
POST /api/v1/auth/register  -> 201 CustomerAuthResponse   anonymous   [auth-register]
POST /api/v1/auth/login     -> 200 CustomerAuthResponse   anonymous   [auth-login]
POST /api/v1/auth/refresh   -> 200 CustomerAuthResponse   anonymous   [auth-refresh]
POST /api/v1/auth/logout    -> 204                        [Customer]
GET  /api/v1/me             -> 200 CustomerResponse       [Customer]
```

```jsonc
// CustomerAuthResponse
{
  "accessToken": "...",
  "accessTokenExpiresAt": "2026-10-07T12:15:00Z",
  "refreshToken": "...",
  "customer": { "id": 42, "email": "an@example.vn", "fullName": "Nguyễn Thị An", "phone": null }
}
```

No `role` — customers do not carry one. The `act` claim is `Customer`.

## What differs from staff

### Register

Sets `email`, `password_hash` and `activated_at` in one step, and logs the customer straight
in. `created_by_staff_id` stays null — this row was self-created.

Duplicate email → **409**. Note `uq_customers_email` is partial on
`email IS NOT NULL AND is_deleted = false`, so a soft-deleted customer's email is free to
reuse and the check must match that, not just "any row with this email".

Body: `email`, `password`, `fullName`, `phone?`. Validate password length (minimum 8) and
`fullName` presence. Slug-style normalization does **not** apply to names.

### A customer may have no credentials at all

`customers.password_hash` and `customers.email` are both nullable: staff can record a
Facebook group member and grant enrollments before any login exists. So login has a state
staff login does not:

| state | response |
|---|---|
| `password_hash` is NULL | **403 "not activated"** — see the open item below |
| `is_active = false` | 403 |
| wrong password | 401, identical to unknown email |

`invited_at` / `activated_at` are written by register only; the invite flow is a later
feature.

### Open item — does 403 "not activated" leak too much?

It confirms the email exists, which a generic 401 would not. The counter-argument is that
these are Facebook group members whose emails are semi-public, and a customer stuck with
"invalid credentials" when they never had a password will just give up or message her.

**This plan assumes the distinct 403.** Flip it to a generic 401 if you would rather not
have the oracle — it is a one-line change in `CustomerAuthService`.

### Longer refresh lifetime

30 days versus 1 day for staff, already in `appsettings.json`. Nobody wants to re-login to
watch a video. The rotation and reuse-detection rules are identical.

## Files

- `ChefLan.Logic/Customers/Interfaces/` — `ICustomerAuthService`; extend
  `ICustomerRepository` and `ICustomerRefreshTokenRepository`
- `ChefLan.Logic/Customers/Services/CustomerAuthService.cs` — `internal sealed`, registered
  by `CustomersModule`
- `ChefLan.Logic/Customers/Contracts/` — `CustomerRegisterRequest`, `CustomerLoginRequest`,
  `CustomerAuthResponse`, `CustomerResponse`
- `ChefLan.Logic/Customers/Validators/`
- `ChefLan.Infrastructure/Repositories/Customers/` — implement both stubs
- `ChefLan.Courses.API/Controllers/CustomerAuthController.cs`, `Controllers/MeController.cs`

Reuse `IJwtTokenService.CreateCustomerToken`, `IPasswordHasher`, `RefreshTokenGenerator`,
`IClock`. The hasher and generator are shared with staff; nothing else is.

## Done when

Everything on plan 3's list, for the customer flow, plus:

- register → 201, logged in immediately, `activated_at` set, `created_by_staff_id` null
- register with an existing email → 409
- register with an email belonging to a **soft-deleted** customer → **201**, because the
  unique index allows it
- register with a 7-character password → 400
- a staff-created customer (`password_hash` NULL) → login **403 "not activated"**, not a 500
  and not a NullReferenceException on BCrypt verify
- `GET /api/v1/me` → 200 with the customer, **no `role` field**
- a **staff** token on `/api/v1/me` → **401** — the mirror of plan 3's check, and just as
  important
- a customer token on `/api/v1/admins/me` → 401
- registering does not create a staff row, and `identity.users` is untouched throughout
