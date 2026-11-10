# Customer profile, province and CSV import — spec

## Context

Chef Lan is moving members of her Facebook groups onto the website. Staff need to create them quickly, from a spreadsheet,
and have each one receive the invitation mail. Every customer should then leave a phone number and province. The customer
record carries a redundant "Facebook name" next to the full name, and the website shows three legal pages that are not
needed yet.

## Decisions (confirmed)

| | |
|---|---|
| Name | Keep `customers.full_name`, labelled **"Tên"** everywhere. **Drop `customers.facebook_name`** (column and code) |
| Email | Required in the admin on create **and** update (existing rows without email need one when next edited) |
| Province | New nullable `customers.province_code`. **34 provinces/cities** (since 1 July 2025), a fixed list in code with ASCII slug codes (`ha-noi`, `da-nang`, `ho-chi-minh`, ...), served by `GET /api/v1/provinces` to web and admin. Optional in the admin, required for the customer |
| CSV import | Admin page. The browser parses the file and creates the customers **one row at a time** through the existing create API with `sendInvite: true`, with a progress bar and a result per row. An email that already exists is skipped. No backend batch job |
| CSV template | `email,ten,facebook` header, UTF-8 with BOM (Excel shows Vietnamese correctly), one example row. Downloadable from the import page |
| Profile completion | Customer with no phone **or** no province: after every login (including right after setting the password) they land on `/account/profile`, and every other page sends them there until both are filled. Name is editable there too |
| Legal pages | Remove `/terms`, `/privacy`, `/refund` from the website for now |

**Breaking migration (as asked to be told):** dropping `facebook_name` deletes the Facebook names entered so far, and the
previous API image cannot run against the new schema. A rollback after this release would need the column added back.

## Plans

| # | Repo | Plan | Status |
|---|---|---|---|
| B1 | chef-lan | [Province, profile, required email, drop facebook_name](plans/01-province-profile.md) | done |
| A | chef-lan-admin | `docs/plans/22-customer-province-csv.md` | todo |
| W | chef-lan-web | `docs/plans/09-profile-completion-no-legal.md` | todo |

## Order of work (Sonnet agents, I review and test)

1. Backend (alone; the other two depend on its contracts).
2. Admin and web in parallel.
3. I test end to end on the local API and local DB: create with required fields; CSV import of a small file (valid,
   invalid, duplicate rows) with SMTP pointed at nothing real — **invites must not reach real addresses in tests**, so the
   import is tested with `Smtp:Host` unset (logging sender) or `@example.com` addresses only; accept an invite on the
   website, get gated to the profile page, save it, then browse freely; legal URLs 404.
4. Local commits per repo; push only when asked. Tell the user before deploying the breaking migration.

## Verification

- `dotnet build ChefLan.slnx -c Release -warnaserror`, `dotnet test tests/ChefLan.Logic.Tests`, `make migrate` (local), the
  migration reviewed (add column, drop column).
- Admin `pnpm lint`, `pnpm build`; web `pnpm typecheck`, `pnpm eslint src tests`, `pnpm build` (no API), `pnpm test:e2e` twice.
- Browser (local API 5059 with CORS for the dev ports): admin create without email → blocked; CSV preview flags bad rows;
  run creates and invites the good rows, skips the duplicate; web activation → profile page → saved → home; a second login
  with a complete profile goes straight on; 375 px layouts; local DB restored afterwards.
