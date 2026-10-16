# B3 — Invitations: token, mail with logo, activation API

**Goal:** staff send (or resend) an invite; the customer receives a branded mail, opens the link, sets a password
and is signed in. Needs B1 (sending) and B2 (customers API).

## Schema

Migration `AddCustomerInvitation`: `customers.invite_token_hash varchar(64) NULL`, `customers.invite_expires_at
timestamptz NULL`, partial unique index `uq_customers_invite_token_hash` WHERE `invite_token_hash IS NOT NULL`.
Additive, **not breaking**. Entity: `Customer.InviteTokenHash`, `Customer.InviteExpiresAt`.

## Endpoints

```
POST /api/v1/admins/customers/{id}/invite      -> 200 { invitedAt, inviteExpiresAt }   (staff; send or resend)
POST /api/v1/auth/invitations/inspect          -> 200 { email, fullName }               (public) body { token }
POST /api/v1/auth/invitations/accept           -> 200 SessionResponse (signed in)        (public) body { token, password }
```
`SessionResponse` lives in the API project (`Contracts/SessionResponses.cs`); build it in the controller the way
`CustomerAuthController.Login` does.

- `POST /admins/customers` with `sendInvite: true` creates then invites in one call (if the mail fails, the customer
  is still created; return 201 with `inviteError: "email.send_failed"` so the admin can resend).
- Invite rules: 400 `customer.no_email` without an email; 409 `customer.already_active` if a password is set;
  400 `customer.account_inactive` if deactivated (existing code). New token each time (replaces the old hash), expiry **now + 7 days**,
  `invited_at = now`. Mail failure → 502 `email.send_failed` and the token change is **not** saved.
- `inspect` / `accept`: unknown, used or expired token → 400 `customer.invite_invalid` (one code for all, no detail).
  `accept` validates the password with the registration rules, sets the hash, `activated_at`, `email_confirmed_at`,
  clears the invite columns and failed-login state, then issues tokens exactly like login (reuse the customer token
  issuing code in `CustomerAuthService`). Both use the `auth-login` rate limiter. `Cache-Control: no-store`.
- Contracts/validators in `ChefLan.Logic/Customers`; token generation/hashing reuse the refresh-token helpers.

## The mail

- Config: `Web:PublicBaseUrl` (e.g. `https://<customer-site>`, no trailing slash) → link
  `{PublicBaseUrl}/activate#token={token}`. Startup fails fast in Production if it is missing while SMTP is set.
- Template: `ChefLan.Infrastructure/Services/Email/Templates/` — `InvitationEmail.html` and `.txt` as **embedded
  resources**, simple `{{placeholder}}` replacement with **HTML-encoding** of every value (`fullName` is user input).
  Placeholders: `fullName`, `link`, `expiresAt` (dd/MM/yyyy, Vietnam time `Asia/Ho_Chi_Minh`), `supportPhone` (`0932 046 296`).
- Logo: `logo-email.png` (resize `chef-lan-web/public/logo.png` 640×640 to ~160×160, < 30 KB) as an embedded resource,
  attached inline with `Content-Id: logo`, referenced as `<img src="cid:logo" width="80" height="80" alt="Bon Bocon">`.
- Layout: table-based, inline CSS, max-width 560px, colours from the site (`#2b2a16` olive, `#d3c8b6` beige, `#7a3f21`
  terracotta button). Subject: "Bon Bocon — Mời bạn kích hoạt tài khoản học". Body (vi): greeting with the name, one
  sentence that chef Lan created an account for them to watch the courses, a button "Đặt mật khẩu", the raw link
  below it, "Liên kết có hiệu lực đến {expiresAt}", contact phone. Plain-text part with the same content.
- The renderer is an Infrastructure service behind a Logic port (`IInvitationEmailComposer` or a generic
  `IEmailTemplates`), so Logic never touches HTML files.

## Docs

`CLAUDE.md` (invite flow, `Web:PublicBaseUrl`, Smtp), `deploy/README.md` (`Web__PublicBaseUrl`, `Smtp__*` in `.env`),
`docs/customer-invitations/spec.md` status table.

## Done when (local DB; clean up afterwards)

- Invite a customer with your own address: the mail arrives (Gmail inbox, not spam), logo visible, button opens
  `/activate#token=...`. Resend → the old link is rejected, the new one works.
- `inspect` returns name/email; `accept` with a short password → 400 validation; with a valid one → 200 session; the
  same token again → 400 `customer.invite_invalid`; login with the new password works.
- Expired token (set `invite_expires_at` in the past) → 400. Invite an active customer → 409; no email → 400.
- Mail failure (wrong app password) → 502 and the previous invite still works.
- Migration applied locally; `make script` contains it. Release build clean.
