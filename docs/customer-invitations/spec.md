# Customer management and invitations — spec

Staff create customer accounts in the admin and invite them by email. The customer clicks a link, sets a
password, and is signed in. Self-registration stays in the API (the website only hides its button), so this adds
the invite path next to it, it does not replace it.

Implementation is split across three repos. Order matters:

| # | Where | Plan | Status |
|---|---|---|---|
| B1 | chef-lan (this repo) | [Real email sending through Gmail](plans/01-email-sending.md) | todo |
| B2 | chef-lan | [Admin customers API](plans/02-admin-customers-api.md) | done |
| B3 | chef-lan | [Invitations: token, mail with logo, activation API](plans/03-invitations-and-activation.md) | todo |
| W1 | chef-lan-web | `docs/plans/01-invite-only-signup.md`: hide the register link, add the activation page | todo |
| A* | chef-lan-admin | `docs/plans/10`–`14`: categories, customers, invite, access, logo | todo |

B1–B3 first; W1 needs B3; the admin customer plans need B2 (and B3 for the invite buttons).

## Decisions (confirmed)

| | |
|---|---|
| Mail provider | **Gmail SMTP with an app password** (`smtp.gmail.com:587`, STARTTLS). Sender = that Gmail address. Gmail limit ~500 mails/day: fine for invites |
| Invitation link | `https://<site>/activate#token=<token>`. Token is in the **fragment** so it never reaches nginx/API access logs. **Valid 7 days, single use.** A new invite replaces the previous one |
| After setting the password | the customer is **signed in** (same session response as login) and the account is activated |
| Mail template | HTML + plain-text alternative, **Bon Bocon logo embedded inline (CID)** so it shows without loading remote images, Vietnamese text, a button to the link, the expiry date |
| Self-registration | API unchanged; the website **only hides** the register link (W1) |
| Admin customer features | list + search + status filter; create (optionally invite right away); edit; deactivate / reactivate (deactivate signs them out everywhere); send / resend invite; view access and grant/revoke (existing enrollment endpoints) |

## Data

On `customers.customers` (one pending invite per customer, so columns are enough):

| column | type | notes |
|---|---|---|
| `invite_token_hash` | `varchar(64) NULL` | SHA-256 hex of the token, like refresh tokens. Never store the raw token |
| `invite_expires_at` | `timestamptz NULL` | |

`invited_at` and `activated_at` already exist. Partial unique index `uq_customers_invite_token_hash` WHERE
`invite_token_hash IS NOT NULL`. **Migration is additive (nullable columns), not breaking.**

Customer status, derived (not stored), used by the admin list:

| status | rule |
|---|---|
| `Inactive` | `is_active = false` |
| `Active` | has a password (`activated_at` set or `password_hash` not null) |
| `Invited` | no password, `invite_token_hash` set and not expired |
| `InviteExpired` | no password, invite expired |
| `NotInvited` | no password, never invited (e.g. a Facebook-era record, or no email) |

## Security rules

- Token: 32 random bytes, base64url. Store only the SHA-256 hash. Compare by hash lookup.
- Activation answers the same 400 `customer.invite_invalid` for unknown, used or expired tokens.
- Setting the password: same password rules as registration; clears the token columns, sets `activated_at`,
  `email_confirmed_at` (they proved the mailbox), resets failed logins.
- Changing a customer's email in the admin clears any pending invite (the old mailbox must not activate it).
- Deactivating revokes all of the customer's refresh tokens.
- Rate limit the public activation endpoints with the existing `auth-login` limiter.
