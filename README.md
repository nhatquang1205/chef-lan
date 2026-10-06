# ChefLan Backend

Backend for chef Lan's online cooking courses. Replaces the current process — one Facebook
group per course, videos posted in the group, customers added by hand — with a real system
of record that stores courses, lessons and the video links themselves.

Also serves the **internal** side of the business, so there are two separate kinds of
account from day one.

## Status

**Step 1 complete**: solution structure + database schema. The catalog endpoint works end
to end. Auth endpoints, admin CRUD and the customer learning endpoints are step 2.

## Stack

.NET 10 · ASP.NET Core (MVC controllers) · PostgreSQL 17 · EF Core 10 · JWT · Serilog · Scalar

## Architecture — Clean Architecture, dependencies point inward

```
ChefLan.Domain         -> nothing     entities + enums, no dependencies at all
ChefLan.Commons        -> nothing     Result, paging, clock, slug, VND, JWT
ChefLan.Logic          -> Domain, Commons              use cases + PORTS (interfaces)
ChefLan.DB             -> Domain, Commons              persistence model ONLY
ChefLan.Infrastructure -> Domain, Commons, Logic, DB   adapters: implements the ports
ChefLan.Courses.API    -> all of the above             controllers, composition root
```

`ChefLan.Logic` declares ports such as `ICourseRepository` and `IEmailSender`;
`ChefLan.Infrastructure` implements them. **Ports never expose `IQueryable`** — that would
leak the ORM back into the application layer, which is the one thing this layout exists to
prevent. Controllers inject Logic interfaces, never `DbContext`.

The two outer projects are split by role:

| | holds | references Logic? |
|---|---|---|
| `ChefLan.DB` | DbContext, entity configurations, interceptors, migrations | **no** |
| `ChefLan.Infrastructure` | repositories, seeder, email / storage / video services | yes |

**Use-case services stay in `ChefLan.Logic`.** A use case (grant an enrollment, log a
customer in) is application logic; a technical service (send mail, store a file, sign a URL)
is an adapter. Only the latter lives in Infrastructure.

Query predicates that carry business meaning — what "published" or "newest first" means —
live in `ChefLan.Logic/Courses/Queries/`, as pure LINQ over `IQueryable<T>` (BCL, not EF),
so Logic has no ORM dependency. The one Postgres-specific predicate, `ILIKE` search, is
applied inline in `CourseRepository`.

Each layer uses a **folder per bounded context** (`Identity/`, `Customers/`, `Courses/`).
Rules that keep it from rotting:

1. One `public static` DI entry point per domain folder; services are `internal sealed`.
2. No cross-domain `using` — `Logic.Courses.*` must not reference `Logic.Customers.*`.
3. Nothing at the root of `ChefLan.Logic` except `Abstractions/` and `DependencyInjection/`.
4. Splitting out a service later means moving matching folders out of Domain/Logic/DB.

A future `ChefLan.Restaurants.API` references the same five inner projects.

## Two auth flows — staff and customers never mix

| | `identity.users` | `customers.customers` |
|---|---|---|
| who | Admin, Staff (internal) | buyers (external) |
| schema | `identity` | `customers` |
| JWT scheme | `Internal` | `Customer` |
| audience | `cheflan-internal` | `cheflan-customer` |
| signing key | `Jwt:Internal:SigningKey` | `Jwt:Customer:SigningKey` |
| `act` claim | `Staff` | `Customer` |
| routes | `/api/v1/admin/**` | everything else |
| self-registration | no — an Admin creates them | yes |
| credentials | always | **optional** |

There is **no foreign key between them, in either direction**. They are disjoint
populations. A staff member who wants to buy a course registers a separate customer
account — which is correct, because the two carry different authority and are audited
separately.

**`sub` means different things under the two schemes** — a user id under `Internal`, a
customer id under `Customer`. If both ever validated with the same key, a customer token
with `sub=1` would be an admin token. Four defences, deliberately redundant:

1. **Different signing keys.** The one that actually matters — a customer token cannot even
   be validated by the internal scheme.
2. **Different audiences**, both validated.
3. **An `act` claim** required by every authorization policy.
4. **Every policy pins `AddAuthenticationSchemes(...)`**, so a customer token on an admin
   route fails authentication rather than falling through to a role check.

Do **not** add a "resolve the current principal" helper that works for both.
`ICurrentStaffContext` and `ICurrentCustomerContext` are separate on purpose, and each
returns null unless the matching `act` claim is present.

## Customers can exist without credentials

`customers.email` and `customers.password_hash` are both nullable. Staff can record every
member of an existing Facebook group — name, FB profile, phone, what they paid — and grant
enrollments immediately, with no login. They can sign in once an invite is accepted and a
password is set.

So **`enrollments.customer_id` points at a customer, not a user.** There is no
user → customer resolution step, because there is nothing to resolve.

## Database

Three schemas, all owned by `CoursesDbContext` for now:

| schema | holds | extracts into |
|---|---|---|
| `identity` | staff accounts + refresh tokens | an internal staff/SSO service |
| `customers` | customer accounts + refresh tokens | a customer identity service |
| `courses` | courses, lessons, categories, enrollments | the Courses service |

Invariants (these are what make a future service split cheap):

- A DbContext maps only tables in schemas it owns.
- **No FK crosses a schema boundary unless one context owns both sides.**
  `courses.enrollments -> customers.customers` is legal today. A future `restaurants.*`
  row pointing at `customers.customers` is not — it learns identity from JWT claims.
- **Nothing in `identity` references `customers`, or the reverse.**
- `enrollments.granted_by_staff_id` and `customers.created_by_staff_id` hold a
  `identity.users.id` **with no FK**, so `courses` has no hard dependency on `identity`.

### Conventions

- **snake_case** (`EFCore.NamingConventions`). Postgres folds unquoted identifiers to
  lowercase, so PascalCase would mean quoting every column in ad-hoc `psql`.
- **`bigint` identity PKs.** Public URLs use slugs, so ids are never enumerated.
- **Enums are int-backed**, with a `CHECK` pinning the range and a column comment
  documenting the values. **Never renumber them.**
- **`timestamptz` only, UTC only**, always from `IClock`. Never `DateTime.Now` — Npgsql
  throws on non-UTC writes, which is a useful guard rail.
- **`numeric(12,0)` for VND** — no minor unit. Note Postgres *rounds* a fractional amount
  rather than rejecting it, so `Vnd.IsValid` is what actually rejects one.
- **Every unique index on a soft-deletable table is partial** on `is_deleted = false`.
  Without that, a deleted course's slug could never be reused.
- `lessons (course_id, sort_order)` is **deliberately not unique** — a partial unique index
  cannot be `DEFERRABLE` in Postgres, which would break drag-and-drop reordering.
  Uniqueness is owned by the reorder use case in one transaction.

### Slugs

`VietnameseSlugGenerator` uses an **explicit Vietnamese character map**, not Unicode NFD.
Under invariant globalization (no ICU — the default in some trimmed and Alpine container
images) `string.Normalize(FormD)` *silently* returns the composed string instead of
throwing, which turns "Món Bắc" into `m-n-b-c`. Slugs are SEO URLs, so a silent
deployment-dependent corruption is the worst possible failure. Verified in both modes.

## Getting started

```bash
docker compose up -d        # Postgres on 5432, pgAdmin on 5050
make migrate                # apply migrations
dotnet user-secrets set "Jwt:Internal:SigningKey" "$(openssl rand -base64 48)" --project src/ChefLan.Courses.API
dotnet user-secrets set "Jwt:Customer:SigningKey" "$(openssl rand -base64 48)" --project src/ChefLan.Courses.API
dotnet user-secrets set "Seed:AdminPassword"      "$(openssl rand -base64 18)" --project src/ChefLan.Courses.API
make run
```

The two signing keys **must differ**. Startup fails if either is missing or under 32 bytes.

Without `Seed:AdminPassword` the seeder logs a warning and creates no Admin account.

- API: `http://localhost:5059`
- Scalar: `http://localhost:5059/scalar/v1`
- Liveness: `/api/v1/health` · readiness incl. Postgres: `/health`

`make help` lists every target.

## Migrations

Migrations live in `ChefLan.DB/Migrations/Courses/`; the startup project is the API.

```bash
make migration NAME=AddPayments   # create
make migrate                      # apply locally
make script                       # idempotent SQL for a production deploy
```

`dotnet-ef` is pinned to 10.0.12 as a **local** tool (`dotnet-tools.json`) — a global
install older than 10 cannot load `net10.0` projects.

**`Database.Migrate()` is never called outside Development.** It races across replicas and
needs DDL rights at runtime. Production applies the reviewed output of `make script`.

## Known gaps

- Search is accent-sensitive `ILIKE`, so typing `pho` does not match `Phở`. Postgres has no
  Vietnamese dictionary; the fix is searching the (already unaccented) slug, or
  `unaccent` + a GIN index.
- Video links are handed out as stored. `IVideoLinkResolver` is the seam where signed or
  domain-restricted URLs go when link sharing becomes a problem — see its doc comment.
