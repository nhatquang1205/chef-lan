# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Backend for chef Lan's online cooking courses, replacing a manual process (one Facebook
group per course, videos posted there, customers added by hand) with a real system of
record. Courses, lessons and video links live in this backend. It also serves the
**internal** side of the business, which is why there are two separate account populations.

**Status**: step 1 (solution structure + schema) is complete and verified. Auth endpoints,
admin CRUD and customer learning endpoints are step 2. The approved step-1 plan is at
`~/.claude/plans/i-want-to-build-zany-elephant.md` and carries the reasoning behind most
decisions below.

## Commands

`make help` lists every target. The important ones:

```bash
docker compose up -d                 # Postgres :5432, pgAdmin :5050
make build                           # dotnet build (solution is ChefLan.slnx)
make run                             # runs the API — port 5059, from launchSettings.json
make migrate                         # apply migrations locally
make migration NAME=AddPayments      # create a migration
make migration-remove                # drop the last unapplied migration
make script                          # idempotent SQL artifact for production deploys
make psql                            # psql shell on the local database
make reset                           # drop + recreate local DB (destroys local data)
```

Always go through `make` for EF commands. A bare `dotnet ef` needs four flags
(`--project src/ChefLan.DB`, `--startup-project src/ChefLan.Courses.API`, `--context
ChefLan.DB.Contexts.CoursesDbContext`, `--output-dir Migrations/Courses`) and silently does
the wrong thing without them.

`dotnet-ef` is pinned to **10.0.12 as a local tool** in `dotnet-tools.json`, so invoke it as
`dotnet dotnet-ef`. A globally installed `dotnet-ef` older than 10 cannot load `net10.0`
projects and fails with a confusing assembly-load error.

### Tests

There are no test projects yet. When added, they go under `tests/`, and a single test runs
with `dotnet test --filter "FullyQualifiedName~MyTest"`. Prefer `Testcontainers.PostgreSql`
over EF InMemory — InMemory ignores query filters, partial indexes and `numeric` precision,
which is precisely what this schema relies on.

### Local secrets

Startup fails fast if either signing key is missing or under 32 bytes:

```bash
dotnet user-secrets set "Jwt:Internal:SigningKey" "$(openssl rand -base64 48)" --project src/ChefLan.Courses.API
dotnet user-secrets set "Jwt:Customer:SigningKey" "$(openssl rand -base64 48)" --project src/ChefLan.Courses.API
dotnet user-secrets set "Seed:AdminPassword"      "$(openssl rand -base64 18)" --project src/ChefLan.Courses.API
```

The two JWT keys **must differ** (see Auth below). Without `Seed:AdminPassword` the seeder
logs a warning and creates no Admin. After adding a `UserSecretsId` to a csproj, rebuild —
`dotnet run --no-build` uses a stale assembly and the secrets silently do not load.

Package versions live in `Directory.Packages.props` (Central Package Management). Add
packages with no `Version` attribute in the csproj.

## Architecture

Clean Architecture, six projects, dependencies strictly inward:

```
ChefLan.Domain         -> nothing     entities + enums; NO project or package references
ChefLan.Commons        -> nothing     Result, paging, IClock, slug, VND, both JWT schemes
ChefLan.Logic          -> Domain, Commons              use cases + PORTS (interfaces)
ChefLan.DB             -> Domain, Commons              persistence model ONLY
ChefLan.Infrastructure -> Domain, Commons, Logic, DB   adapters: implements the ports
ChefLan.Courses.API    -> all of the above             controllers, composition root
```

**`ChefLan.DB` vs `ChefLan.Infrastructure`** — the split is deliberate:

- `ChefLan.DB` is the persistence model and nothing else: `CoursesDbContext`, entity
  configurations, interceptors, migrations, `ChefLanSchemas`. It **does not reference
  `ChefLan.Logic`** and must stay that way — it knows no ports and no use cases.
- `ChefLan.Infrastructure` is the adapter layer: repositories over that DbContext, the
  seeder, and technical services (email, file storage, signed video links). It is the only
  project that binds a Logic port to a concrete implementation.

**Use-case services do NOT live in Infrastructure.** "Service" means two different things:
a use case (grant an enrollment, log a customer in) is application logic and belongs in
`ChefLan.Logic/<Domain>/Services/`; a technical service (send mail, store a file, sign a
URL) is an adapter and belongs in `ChefLan.Infrastructure/Services/`.

`Program.cs` calls `AddChefLanCommons()`, `AddChefLanLogic()` and
`AddChefLanInfrastructure(...)`. The last one calls `AddCoursesDataAccess(...)` from
`ChefLan.DB` internally, because DB owns the DbContext so DB owns registering it.

A future `ChefLan.Restaurants.API` references the same five inner projects.

Rules that are easy to violate and expensive to undo:

- **EF Core belongs in `ChefLan.DB` and `ChefLan.Infrastructure` only.** No `DbContext`
  and no EF type or attribute may appear in Logic, Commons or Domain.
- **Ports never expose `IQueryable`.** Repository interfaces return entities,
  `PagedResult<T>` or `Result<T>`, never a query to be composed by the caller.
- **Query predicates that carry business meaning live in Logic**, in
  `ChefLan.Logic/Courses/Queries/CourseQueryExtensions.cs` — what "published", "in this
  category" and "newest first" mean are application rules, not data access. These are pure
  LINQ over `IQueryable<T>`, which is BCL (`System.Linq`), **not** EF Core, so Logic stays
  ORM-free. Provider-specific predicates are the exception: `EF.Functions.ILike` is
  Npgsql-only and is applied inline in `CourseRepository` rather than as an extension
  method, so it cannot drift into Logic. Keep that rule — it is why `ChefLan.Logic` has no
  EF package reference, and a `dotnet list package` on it should stay that way.
- **Escape LIKE metacharacters** before interpolating user input into an ILIKE pattern.
  `CourseRepository.EscapeLikePattern` does this; without it, searching `%` returns the
  whole catalog. Not injection (EF parameterises the pattern) — just wrong results.
- **`ChefLan.Domain` stays dependency-free.** No packages, no `ChefLan.Commons`. That is
  why entity prices are plain `decimal` and `Vnd` lives in Commons.
- **Controllers inject Logic interfaces, never `DbContext`.** The API references
  `ChefLan.DB` solely so `Program.cs` can call `AddCoursesDataAccess(...)`.
- **No generic `IRepository<T>`**, and no AutoMapper or MediatR (both went commercial in
  2025). Mapping is hand-written `ToResponse()` extensions in `Mapping/` folders.

Each layer uses a **folder per bounded context** (`Identity/`, `Customers/`, `Courses/`):

1. One `public static` DI entry point per domain folder (e.g. `CoursesModule.AddCoursesModule`);
   services are `internal sealed`, so the API cannot resolve them by name.
2. No cross-domain `using` — `Logic.Courses.*` must not reference `Logic.Customers.*`.
3. Nothing at the root of `ChefLan.Logic` except `Abstractions/` and `DependencyInjection/`.
   Code moves to `Abstractions/` only if two domains need it and it names no domain noun.
4. Splitting a microservice out later = moving matching folders out of Domain/Logic/DB.
   Resist adding a sixth project when Commons feels crowded; add a folder instead.

## Auth: two populations that must never mix

`identity.users` is **internal staff** (`Admin`, `Staff`). `customers.customers` is
**external buyers**. They are disjoint: no foreign key either way, no shared credential, no
shared token. A staff member who buys a course registers a separate customer account.

| | staff | customer |
|---|---|---|
| JWT scheme | `Internal` | `Customer` |
| audience | `cheflan-internal` | `cheflan-customer` |
| signing key | `Jwt:Internal:SigningKey` | `Jwt:Customer:SigningKey` |
| `act` claim | `Staff` | `Customer` |
| routes | `/api/v1/admin/**` | everything else |
| self-registration | no — an Admin creates them | yes |

**`sub` means different things under the two schemes** — a user id under `Internal`, a
customer id under `Customer`. If both validated with the same key, a customer token with
`sub=1` would be an admin token. Four deliberately redundant defences, all in
`ChefLan.Commons/Auth/AuthServiceCollectionExtensions.cs`:

1. Different signing keys (the one that actually matters).
2. Different audiences, both validated.
3. An `act` claim required by every policy.
4. Every policy pins `AddAuthenticationSchemes(...)`, so a customer token on an admin route
   fails authentication rather than falling through to a role check.

**Do not add a unified "current principal" accessor.** `ICurrentStaffContext` and
`ICurrentCustomerContext` are separate on purpose and each returns null unless the matching
`act` claim is present. A shared accessor is how those two ids get confused.

`MapInboundClaims = false` keeps the short claim names (`sub`, `role`, `act`).

## Customers without credentials, and why enrollments point at them

`customers.email` and `customers.password_hash` are **both nullable**. Staff can record
every member of an existing Facebook group — name, FB profile, phone, what they paid — and
grant enrollments immediately, with no login. They sign in later once an invite is accepted.

Therefore **`enrollments.customer_id` references a customer, not a user.** There is no
user → customer resolution step because there is nothing to resolve; a customer token's
`sub` *is* the customer id.

Consequence to expect: duplicate customer rows are possible by design, so a staff "merge
customers" action (repoint `enrollments.customer_id`, soft-delete the loser) will be needed
once the Facebook backlog is imported.

## Database

Three schemas, all currently owned by `CoursesDbContext`: `identity` (staff),
`customers`, `courses`. A second context (e.g. `RestaurantsDbContext`) gets its own schema
and its own migrations history table.

Boundary invariants — these are what make a future service split cheap:

- A DbContext maps only tables in schemas it owns.
- **No FK crosses a schema boundary unless one context owns both sides.**
  `courses.enrollments -> customers.customers` is legal today; a future `restaurants.*` row
  pointing at `customers.customers` is not.
- **Nothing in `identity` references `customers`, or the reverse.**
- `enrollments.granted_by_staff_id` and `customers.created_by_staff_id` hold an
  `identity.users.id` **with no FK**, so `courses` has no hard dependency on `identity`.
  Audit columns (`created_by_user_id` etc.) are FK-less for the same reason.

### Conventions that bite if ignored

- **snake_case** via `EFCore.NamingConventions`. Postgres folds unquoted identifiers to
  lowercase, so PascalCase would force quoting in every ad-hoc query.
- **Configuration is one `IEntityTypeConfiguration<T>` per entity** under
  `ChefLan.DB/Configurations/<Domain>/`. **Zero data annotations** — Domain must stay
  EF-free, so the config file is the only place to look. Each calls `ToTable(name, schema)`
  explicitly so the schema seam is visible where the table is owned.
- **Every unique index on a soft-deletable table must be partial** on `is_deleted = false`.
  Without it, a deleted course's slug can never be reused. `uq_customers_email` additionally
  filters `email IS NOT NULL`.
- **`lessons (course_id, sort_order)` is deliberately NOT unique.** A partial unique index
  cannot be `DEFERRABLE` in Postgres, so a unique one breaks drag-and-drop reordering.
  Uniqueness is owned by the reorder use case, rewriting the course's rows in one
  transaction. Do not "fix" this index.
- **Enums are int-backed** with a `CHECK` pinning the range and a column comment
  documenting the values. Assign values explicitly and **never renumber**.
- **`timestamptz` only, UTC only, always from `IClock`.** Never `DateTime.Now` — Npgsql
  throws on non-UTC writes to `timestamptz`; treat that as a guard rail, not an obstacle.
- **`numeric(12,0)` for VND** (no minor unit). Postgres *rounds* a fractional amount rather
  than rejecting it, so `Vnd.IsValid` is what actually rejects one — validate in the
  application layer.
- Soft delete applies to `users`, `customers`, `courses`, `lessons`, `categories`. Not
  `enrollments` (revocation is `status = Revoked`, preserving the grant and money record)
  and not the refresh-token tables (they have `revoked_at`).
- `inet` is not used for IP columns: Npgsql maps it to `IPAddress`, not `string`. These are
  display-only audit fields stored as `varchar(45)`.
- **`Database.Migrate()` is never called outside Development.** It races across replicas and
  needs DDL rights at runtime. Production applies the reviewed output of `make script`.

## Slugs

`VietnameseSlugGenerator` uses an **explicit Vietnamese character map, not Unicode NFD**.
Under invariant globalization (no ICU — the default in some trimmed and Alpine container
images) `string.Normalize(FormD)` *silently* returns the composed string instead of
throwing, turning "Món Bắc" into `m-n-b-c`. This already happened once. Slugs are SEO URLs,
so a silent deployment-dependent corruption is the worst failure mode.

If you touch this class, verify in **both** modes:
`dotnet run` and `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet run`.

Note `đ`/`Đ` are distinct letters, not composed ones, so even working NFD never decomposes
them. Always generate slugs server-side; never trust client input; append `-2`, `-3` on
collision via `GenerateUnique`.

## Known gaps

- **Search is accent-sensitive `ILIKE`**, so typing `pho` does not match `Phở` — and
  Vietnamese users routinely type without accents. Postgres has no Vietnamese dictionary;
  the cheap fix is searching the already-unaccented `slug` column, or `unaccent` + GIN.
- **Video links are handed out as stored.** An unlisted YouTube/Vimeo link is obscurity, not
  access control — once a paying customer sees it, they can paste it into a Facebook group.
  `IVideoLinkResolver` (`Logic/Courses/Video/`) is the single seam where signed short-TTL
  CDN URLs or domain-restricted embeds go. Never return `Lesson.VideoUrl` straight from the
  column in a response.
- **Cross-scheme token rejection is wired but untested**, because step 1 has no protected
  endpoints. Test it when step 2 adds them.
- No rate limiting yet. Do not ship login without `AddRateLimiter` on both
  `/api/v1/auth/*` and `/api/v1/admin/auth/*`.
