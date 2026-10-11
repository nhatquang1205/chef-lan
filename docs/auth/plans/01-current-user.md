# Plan 1 — `ICurrentUser` middleware

**Status: done.** Deviations from the plan as written: `ActorType` moved from
`ChefLan.Domain` to `ChefLan.Commons.Auth` (Commons cannot reference Domain, and nothing used
the Domain copy), so `ICurrentUser.Source` is `ActorType?` — null when unauthenticated. The
interceptors live in `ChefLan.DB/Interceptors/`, not Infrastructure. Verified end to end:
a staff token and a customer token each resolve only their own `TryGet*Id`.

No endpoints, no staff token needed. See [spec.md](../spec.md).

**Goal:** one accessor for both JWT sources, replacing the two separate contexts.

> Moved here from `docs/course-content/plans/02-current-user.md` — it is authentication
> infrastructure, and auth lands before the content CRUD plans that depend on it.

---

## This supersedes a rule in CLAUDE.md

CLAUDE.md currently says *"Do not add a unified current principal accessor"*. That rule is
being replaced — **rewrite it, do not leave the doc contradicting the code.**

The reason it existed still holds, so the shape handles it: `sub` is a staff user id under
the `Internal` scheme and a customer id under `Customer`. Reading an id therefore forces you
to name the population you expect.

```csharp
public interface ICurrentUser
{
    ActorType Source { get; }       // Staff | Customer
    bool IsAuthenticated { get; }
    string? Email { get; }
    string? Role { get; }           // Staff only
    bool IsStaff { get; }
    bool IsCustomer { get; }

    bool TryGetStaffId(out long userId);        // false unless Source == Staff
    bool TryGetCustomerId(out long customerId); // false unless Source == Customer
}
```

There is deliberately **no bare `long Id`**. It would let
`customerRepo.GetByIdAsync(currentUser.Id)` compile with a staff id in it, returning the
wrong row silently — an account-takeover bug, not a typo.

## Middleware ordering matters

The default scheme is `Customer` and the policies pin their scheme, so `HttpContext.User` is
populated by the **authorization** middleware, not `UseAuthentication`. Placing this before
`UseAuthorization()` reads the wrong principal:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseChefLanCurrentUser();   // after UseAuthorization, not before
app.MapControllers();
```

Admin endpoints reject a `Customer`-sourced token. Two layers do this and both stay: the
scheme-pinned policy (different signing keys, so a customer token cannot even validate
against `Internal`), plus an explicit `Source == Staff` guard.

## Files

- `ChefLan.Commons/Auth/` — `ICurrentUser.cs`, `CurrentUser.cs`, `CurrentUserMiddleware.cs`
  + `UseChefLanCurrentUser()`
- **delete** `ICurrentStaffContext.cs`, `ICurrentCustomerContext.cs`,
  `HttpCurrentStaffContext.cs`, `HttpCurrentCustomerContext.cs`
- `ChefLan.Commons/Auth/AuthServiceCollectionExtensions.cs` — swap the two scoped
  registrations for one
- `ChefLan.Infrastructure/Interceptors/{Auditing,SoftDelete}SaveChangesInterceptor.cs` —
  switch to `ICurrentUser.TryGetStaffId`
- `ChefLan.Courses.API/Program.cs` — middleware ordering
- `CLAUDE.md` — rewrite the unified-accessor rule; change `/api/v1/admin/**` to
  `/api/v1/admins/**`

## Done when

- full rebuild, 0 warnings; app boots; `GET /api/v1/courses` still 200
- `grep -rn "ICurrentStaffContext\|ICurrentCustomerContext" src` returns nothing
- audit columns still populate on write — exercise via the seeder, then check `created_at`
  and `created_by_user_id` on a seeded category row
- unauthenticated: `IsAuthenticated` false, **both** `TryGet*` return false
- customer principal: `TryGetCustomerId` true, `TryGetStaffId` **false**
- staff principal: `TryGetStaffId` true, `TryGetCustomerId` **false**

The last two are the point of the plan. There are no protected endpoints yet, so assert them
against a hand-built `ClaimsPrincipal` per scheme rather than inferring from a 401.
