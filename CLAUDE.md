# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Backend for chef Lan's online cooking courses, replacing a manual process (one Facebook
group per course, videos posted there, customers added by hand) with a real system of
record. Courses, lessons and video links live in this backend. It also serves the
**internal** side of the business, which is why there are two separate account populations.

**Status**: the solution structure, schema and **authentication** (staff and customer login,
refresh, logout, `me`, rate limiting) and **content CRUD** (categories, courses, lessons,
lesson videos, publish) are done and verified — see `docs/auth/` and `docs/course-content/`.
Next: enrollments and the customer learning endpoints. The docs carry the reasoning behind
most decisions below.

**Feature docs live in `docs/<feature>/`** — `spec.md` for context, decisions and shared
rules, plus one numbered file per plan under `plans/`. Keep them current as work lands;
`spec.md` carries the plan index and status table.

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

### Configuration precedence

`appsettings.json` -> `appsettings.{Environment}.json` -> **`appsettings.Local.json`** ->
user-secrets -> environment variables -> command line. Later wins.

`appsettings.Local.json` is gitignored and loaded by an explicit `JsonConfigurationSource`
inserted in `Program.cs` after the last `appsettings*.json` source.
`appsettings.Local.example.json` is the committed template — copy it.

**Never set `ASPNETCORE_ENVIRONMENT=Local` to pick that file up.** It loads, but
`IsDevelopment()` becomes false, which silently disables OpenAPI/Scalar, skips the seeder,
and turns off `EnableSensitiveDataLogging`. Keep the environment `Development` and use the file.

On a server, use **environment variables**, with `__` in place of `:`
(`Video__Bunny__ApiKey`). They outrank every JSON file, so a stale `appsettings.Local.json`
in a deploy artifact cannot override them. Do not write secrets into a committed
`appsettings*.json`, and never bake them into a Docker image at build time — image layers
are readable by anyone who can pull the image.

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
| routes | `/api/v1/admins/**` | everything else |
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

**`ICurrentUser` is the one accessor for both populations**, filled by
`app.UseChefLanCurrentUser()`, which **must run after `UseAuthorization()`** — the default
scheme is `Customer` and policies pin their own, so `HttpContext.User` is only the right
principal once authorization has run. It deliberately has **no bare `long Id`**: read an id
with `TryGetStaffId` or `TryGetCustomerId`, each of which is false unless the `act` claim
matches. That is what stops a customer id being used as a staff id. `Source` is null when
unauthenticated. Controllers on a pinned policy still guard with `TryGet*Id` and return 401.

`MapInboundClaims = false` keeps the short claim names (`sub`, `role`, `act`).

**The web client has one login form, so `/api/v1/auth/*` and `/api/v1/me` are a shared entry point.**
`POST /api/v1/auth/login` tries the customer credentials first and, only if the *same* email and
password are not a customer's (`customer.invalid_credentials`), tries them as a staff account — so an
admin signs in on the web with her staff login and gets a normal **Internal** token. The response
(`SessionResponse`) carries `actor: "Customer" | "Staff"` and exactly one of `customer` / `staff`.
`/auth/refresh` falls back to the staff refresh-token table the same way, and `/auth/logout` and `/me`
use the `AnyUser` policy (either scheme) and branch on `ICurrentUser`. Tokens are **not** merged: a staff
token is still signed with the staff key, has the staff audience and `act=Staff`, still cannot be read
as a customer id, and a customer token is still refused on `/api/v1/admins/**`. What is shared is only
the entry point, so the rate limit (`auth-login`) and per-account lockout are too: five wrong passwords
against an admin's email lock the admin out from either login endpoint. Every failing path costs two
password verifications (a miss verifies a dummy hash), so neither status code, body nor timing says
which population an email belongs to. A customer who exists but is locked, inactive or not activated
gets *that* answer and is never retried as staff. Admin endpoints (`/api/v1/admins/**`) are unchanged.
**Staff on the website see everything published.** The lesson page (`GET /courses/{slug}/lessons/{lessonSlug}`) and
playback (`GET /videos/{publicId}/playback`) require a signed-in account (`AnyUser`: customer OR staff token; anonymous
is 401). A staff token gets full access there (every published video unlimited, every recipe) and
`GET /courses/{slug}/access` answers "everything" with `viewerIsStaff: true`. The course list and course page stay
public (`[AllowAnonymous]`). Who is asking is always decided with `ICurrentUser.TryGetStaffId/TryGetCustomerId`.

Login rules, identical for both populations (`ChefLan.Logic/Abstractions/LoginSecurity.cs`):
5 failures lock for 15 minutes (the locked check runs *before* the password check, so the
right password is refused too); unknown email verifies against a dummy BCrypt hash so timing
does not reveal which emails exist; refresh tokens rotate, and presenting an already-rotated
one revokes every token for that account. Refresh tokens are SHA-256 hashed at rest and
travel in the JSON body. A customer with no password gets a distinct 403 `not_activated` —
a deliberate enumeration trade-off, flip it in `CustomerAuthService.LoginAsync` if unwanted.

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

## API conventions

- Admin routes are `/api/v1/admins/...` (plural); customer routes `/api/v1/...` are
  read-only apart from auth. Admin controllers use `[Authorize(Policy = StaffOrAdmin)]`;
  there is no Staff-vs-Admin permission split.
- CORS: policy `Web`, origins from `Cors:AllowedOrigins` (empty = no cross-origin access;
  `http://localhost:3000` in Development; production via `Cors__AllowedOrigins__0`). No
  credentials — tokens are in JSON bodies. The customer web app is a separate repo
  (`chef-lan-web`, see `docs/customer-web/`).
- Enums travel as **strings** (`"status": "InActive"`), via `JsonStringEnumConverter`, registered for both MVC and the minimal-API options (OpenAPI
  reads the latter, so without both the API document would describe enums as numbers).
- Failures are `Result<T>` -> `ResultToActionResultExtensions`; validation is FluentValidation
  in `ChefLan.Logic/**/Validators/`, run by `ValidationFilter` (400 `ValidationProblemDetails`).
- A save that loses a race (stale `xmin`, or a unique index hit concurrently) throws
  `ConflictException` from `UnitOfWork`, which the exception handler turns into a 409.
- Slugs are generated on create and **never change on rename**; unique per scope with `-2`,
  `-3`. Denormalized counters (`lesson_count`, `video_count`, durations) are recomputed in
  memory from tracked entities and saved in the same `SaveChanges` as the write.
- Reorder endpoints take the full ordered id list and 400 unless it is exactly the parent's
  live children. Public video URLs always go through `IVideoLinkResolver`; a non-free video
  has no `url` key in the response at all.

## Deployment

Production runs as a container (`Dockerfile`) behind nginx on the same domain as the website
(`/api/` -> `127.0.0.1:5100`), next to the shared Postgres container. `deploy/` holds the compose
file, `deploy-api.sh` and the VPS setup guide; `.github/workflows/deploy.yml` builds, pushes to GHCR
and deploys. Design: `docs/deploy-api/`. Things that bite:

- **Secrets are environment variables** from a `.env` on the VPS (`__` for `:`), never in the image.
  The two JWT keys must differ and must not be the dev ones.
- **The first Admin** is created only when `Seed__RunOnStartup=true` (idempotent, creates nothing
  but an Admin if none exists). Set it for the first deploy, then remove it.
- **Rate limiting depends on `ForwardedHeaders__KnownProxies__0`** being the Docker network gateway
  (compose pins `172.30.10.1`). Wrong or missing, every visitor shares one login budget (5/min for
  the whole site). nginx must *overwrite* `X-Forwarded-For`, not append.
- Migrations are applied by the deploy from the generated idempotent SQL, as the app's own database
  role, before the new container starts. Keep migrations backwards-compatible for one release:
  rollback re-deploys the previous image against the newer schema.
- CORS is empty in production (same origin); only Development allows `http://localhost:3000`.

## Videos: public id, playback endpoint

- `lesson_videos.public_id` (16 hex, generated by Postgres, partial unique) is the **only** video id the
  public site and API use. Never expose the integer `id` (sequential: enumerable) or the Bunny GUID /
  `external_id` (provider data). Admin responses carry both `id` and `publicId`.
- The public course outline (`GET /courses/{slug}`) carries **no playable links and no integer ids** for
  videos: just `publicId`, `isFree`, `freeTillSeconds`. A link is minted per play by
  `GET /api/v1/videos/{publicId}/playback` (`Cache-Control: no-store`), which answers 404 unless the video,
  its lesson and its course are all published, and 403 unless the video is free (full access comes with
  enrollments). `expiresAt` comes from `IVideoLinkResolver.LinkLifetime`.
- `freeTillSeconds` is enforced by the web watch page through Bunny's player.js; the API does not and
  cannot enforce it (see `docs/video-player/spec.md`).

## Lesson prices and access

Spec: `docs/lesson-access/spec.md`. `lessons.price_vnd` is optional (NULL = only sold inside the course).
Access has two tables: `enrollments` (whole course, includes every lesson) and `lesson_enrollments` (one lesson,
same shape and rules, period = the lesson's course's `access_duration_days`). To watch a non-free video a
customer needs an active, unexpired row in either, checked by `IEnrollmentRepository.HasAccessAsync` inside
`PlaybackService`; owners also lose the `freeTillSeconds` limit. Staff grant by hand through
`/api/v1/admins/enrollments` and `/lesson-enrollments` (no checkout exists). `ICustomerDirectory` is the
Courses domain's own port onto customers, so granting never reaches into `Logic.Customers`. A grant that is
still active is a 409; revoked or expired rows are reactivated.
**Add-on lessons:** `lessons.included_in_course = false` means the lesson is not part of the course price: a course
enrollment does not unlock it (only a `lesson_enrollments` row does), it requires a price, and it is excluded from
`lesson_count` / `total_duration_seconds`. Details in `docs/lesson-access/spec.md`.
`GET /api/v1/courses/{slug}/lessons/{lessonSlug}` is the public lesson page (outline, videos, prev/next); the
**recipe is returned only to a customer with access** (`recipesLocked` tells the page there is one), so the answer is
`Cache-Control: private, no-store`. Playback responses carry `lessonSlug` so old watch links can redirect there.
`GET /api/v1/me/courses` (customer only) is the library: published courses owned whole (`hasCourseAccess`, `courseExpiresAt`) or in part (`lessons`), active and unexpired only, newest grant first.

## Email

`IEmailSender` (Logic port) takes an `EmailMessage` (HTML + plain text + inline images referenced as `cid:`).
`SmtpEmailSender` (MailKit) is used whenever `Smtp:Host` is set, in **any** environment, so Gmail can be tested
locally with user-secrets; otherwise `LoggingEmailSender` logs the mail (the text body only in Development, since it
may carry a one-time link; elsewhere just a warning). Production uses Gmail with an app password (`deploy/README.md`,
"Email setup"). Failures throw `EmailSendException`; nothing is queued or retried. `System.Security.Cryptography.Pkcs`
is pinned in `Directory.Packages.props` because MimeKit pulls in a vulnerable version: keep the pin until MimeKit
references a fixed one.

## Customer invitations

Spec: `docs/customer-invitations/spec.md`. Staff create customers (`/api/v1/admins/customers`) and invite them
(`POST /admins/customers/{id}/invite`, or `sendInvite: true` on create). The invite is a one-time token: stored only
as a hash in `customers.invite_token_hash`, valid 7 days (`invite_expires_at`), replaced by every resend, cleared on
use and when staff change the email. The mail (`CustomerEmails`, embedded templates + inline logo under
`Infrastructure/Services/Email/Templates`) links to `{Web:PublicBaseUrl}/activate#token=...` — the token is in the
**fragment** so it never reaches access logs. The website calls `POST /api/v1/auth/invitations/inspect` and
`/accept`; accept sets the password, activates the account and returns the normal session. Both answer one code,
`customer.invite_invalid`, for unknown, used and expired tokens. The mail is sent before the token is saved, so a
failed send (`502 email.send_failed`) leaves the previous link working. Customer status (`Active`, `Invited`,
`InviteExpired`, `NotInvited`, `Inactive`) is derived in `CustomerMappings.StatusOf` and mirrored in
`CustomerRepository.ListAsync`; change both together.

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
- **Video delivery is Bunny Stream, and it is only active when configured.** With
  `Video:Bunny` set, `BunnyVideoLinkResolver` mints a short-lived signed embed URL per
  request; without it, DI falls back to `PassthroughVideoLinkResolver`, which hands back
  the stored column — fine locally, **never safe in production**, because an unsigned URL
  is a permanent shareable link. Always go through `IVideoLinkResolver`; never return
  `Lesson.VideoUrl` straight from the column in a response.

  Bunny's embed token is `SHA256_HEX(TokenSecurityKey + videoId + expiresUnixSeconds)`,
  lowercase hex. It is **time**-bound, not viewer-bound — the formula has no customer
  field — so keep `TokenLifetimeSeconds` short and set a referer allowlist in the Bunny
  dashboard. Two distinct Bunny secrets exist and are easy to confuse:
  `TokenSecurityKey` signs playback, `ApiKey` is the `AccessKey` header for REST calls.
  Neither may reach the browser.

  A lesson marked `VideoProvider.Bunny` with no `VideoExternalId` resolves to `null`
  (fails closed) rather than falling back to `VideoUrl`, which would leak an unsigned link.

- **Course create/update also come as multipart/form-data** (`CourseFormRequest`) next to the JSON form:
  same fields, plus the thumbnail as `thumbnailUrl` **or** `thumbnailFile` (both = 400
  `course.thumbnail_conflict`); `categoryIds` is a repeated field. The form is validated *before* anything
  is uploaded, and an image whose course then fails to save (e.g. unknown category) is deleted again. A
  multipart PUT, like the JSON PUT, replaces the whole course: omit both thumbnail fields and the thumbnail
  is cleared. Replacing a thumbnail leaves the old image on Bunny (no cleanup). **Form values are parsed
  with the request culture**, so `Program.cs` pins it to invariant (`UseRequestLocalization`): without
  that, `1200000.50` was silently 120,000,050 on a vi-VN machine. Never remove it. nginx must allow a
  6 MB body on `/api/` for this.
- **Uploads go through this API, by decision** (`POST /api/v1/admins/uploads/images|videos`,
  staff only). **Images** take either the raw image as the request body (Postman: Body -> binary) or
  multipart with a `file` field; **videos** take multipart only. Images (JPEG/PNG/WebP, 5 MB, validated by file *content*, never the
  client's name or type; SVG refused) go to a public **Backblaze B2** bucket (S3-compatible API via AWSSDK.S3, same as wish-list; Bunny is video only) and the response is the object's public URL.
  Videos are **streamed**: the controller reads the multipart body with `MultipartReader` (MVC's form
  binding is switched off for that action, or it would buffer the whole file) and forwards it to Bunny
  Stream as it arrives. Measured: a 381 MB upload raised API memory by ~12 MB and used no temp disk.
  Keep it that way: no `IFormFile`, no `ReadFormAsync`, no buffering on this path. The video's id
  (`videoId`) goes into a lesson video's `externalId`. A failed or aborted upload deletes the empty
  Bunny video it created. **Two things to watch on the VPS:** nginx must have
  `proxy_request_buffering off` on `/api/v1/admins/uploads/` (see `deploy/nginx-api.conf`), or it
  spools the whole file to disk first; and Bunny's video `PUT` is sent with chunked transfer
  encoding (the docs do not say whether `Content-Length` is required). If Bunny ever rejects that,
  add a `size` form field and set the content length from it.
- **No automated tests yet.** Cross-scheme rejection (customer token on `/admins/*`, staff
  token on `/me`, both 401), refresh-token replay and lockout were verified by hand with curl
  against a running API. They are the security boundary and deserve Testcontainers tests.
- **Rate limiting is per-process and in-memory** (`auth-login` 5/min, `auth-register` 3/min,
  `auth-refresh` 20/min, by IP and by submitted email; limits in `appsettings.json` under
  `RateLimiting`). Several replicas multiply the budget. Behind a proxy, set
  `ForwardedHeaders:KnownProxies` — without it `RemoteIpAddress` is the proxy and every
  client shares one IP bucket; never enable forwarded headers without restricting proxies.
- `POST /api/v1/admins/auth/change-password` clears `must_change_password` and revokes all
  of the user's refresh tokens (the caller included — they log in again). Nothing *enforces*
  the flag yet: a staff member with it set can still use every endpoint.
