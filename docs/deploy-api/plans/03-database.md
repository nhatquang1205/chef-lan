# Plan 3 — Database

**Status: done.** Applied by `deploy/deploy-api.sh` as the app role. Not one transaction: EF's script has its own BEGIN/COMMIT per migration, so `--single-transaction` only produced warnings and was removed. Needs the Postgres container details (spec, "Information needed").

## One-time, in the existing Postgres

A **separate database and a separate login** for chef-lan — never wish-list's:

```sql
CREATE ROLE cheflan LOGIN PASSWORD '<generated>';
CREATE DATABASE cheflan OWNER cheflan ENCODING 'UTF8';
-- inside the new database:
CREATE EXTENSION IF NOT EXISTS citext;   -- needs a superuser; the schema uses citext for email
```

Run via `docker exec -i <postgres-container> psql -U <superuser>`. `CREATE EXTENSION citext` is the
one step that needs superuser; after that the `cheflan` role needs no elevated rights.

Connection string for the API (in the VPS `.env`):
`ConnectionStrings__ChefLanDb=Host=<postgres-container>;Port=5432;Database=cheflan;Username=cheflan;Password=…`
— the host is the container name, reachable because the API joins the same Docker network.

## Applying the schema

`make script` produces an **idempotent** SQL file (every migration guarded by
`__EFMigrationsHistory`), so re-applying is safe. Two ways to run it:

| | |
|---|---|
| **Automatic — decided** | CI generates the SQL; the deploy pipes it into `psql` in the Postgres container (`ON_ERROR_STOP=1`) *before* the new container starts. Same idea as wish-list's `npm run db:init` |
| Manual | an operator reviews and applies it. Matches the CLAUDE.md wording ("the reviewed output of `make script`") but a forgotten step means the app starts against an old schema |

Risk to be aware of with automatic: a destructive migration (a dropped column) would apply without
a human reading it. Mitigation: migrations are reviewed in the PR; CI uploads the generated SQL as an
artifact for reading.

## Done when

- the `cheflan` role cannot connect to wish-list's database (and vice versa)
- the schema script applies cleanly to an empty database and again (idempotent) with no changes
- `\dn` shows `identity`, `customers`, `courses`; `SHOW timezone` is UTC
