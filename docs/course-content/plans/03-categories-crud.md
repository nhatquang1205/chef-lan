# Plan 3 — Categories CRUD

**Status: done.** `DELETE` 409s while a non-deleted course uses the category. The slug is generated on create and left alone on rename, so links do not break. Enums (`status`, `level`, …) now serialize as **strings** across the whole API. Needs a staff token — see the gate in [spec.md](../spec.md).

**Goal:** 5 admin + 1 customer endpoint. This plan establishes the
controller / service / validator pattern that plans 4–6 copy, so get it right here.

---

## Endpoints

```
GET    /api/v1/categories                   -> CategoryResponse[]   anonymous, Active only
GET    /api/v1/admins/categories?status=&q= -> CategoryResponse[]
GET    /api/v1/admins/categories/{id}       -> CategoryResponse
POST   /api/v1/admins/categories            -> 201 CategoryResponse
PUT    /api/v1/admins/categories/{id}       -> CategoryResponse
DELETE /api/v1/admins/categories/{id}       -> 204                  soft delete
```

Body: `name`, `description?`, `thumbnailUrl?`, `displayOrder?`, `status`.

- **`slug` is generated** from `name` via `ISlugGenerator.GenerateUnique`, never accepted
  from the client. Collisions get `-2`, `-3`.
- `status` and `displayOrder` are ordinary PUT fields — no separate activate or reorder
  endpoints in this step.
- `DELETE` returns `409` while a non-deleted course references the category. The message
  must name InActive as the reversible alternative, because that is almost always what the
  caller actually wanted.

## Files

- `ChefLan.Logic/Courses/Interfaces/ICategoryRepository.cs` — extend (list with status
  filter and search, slug existence, in-use check)
- `ChefLan.Logic/Courses/Interfaces/` — `ICategoryService`, `ICategoryCatalogService`
- `ChefLan.Logic/Courses/Services/` — `internal sealed` implementations, registered by
  `CoursesModule`
- `ChefLan.Logic/Courses/Contracts/{Requests,Responses}/` — create/update request,
  `CategoryResponse` already exists
- `ChefLan.Logic/Courses/Validators/` — FluentValidation per request
- `ChefLan.Infrastructure/Repositories/Courses/CategoryRepository.cs` — implement the stub
  (currently throws `NotImplementedException`)
- `ChefLan.Courses.API/Controllers/CategoriesController.cs`,
  `Controllers/Admin/AdminCategoriesController.cs`
- `ChefLan.Courses.API/Infrastructure/ValidationFilter.cs` — **declared in step 1, never
  built.** First real use is here: FluentValidation failures → 400
  `ValidationProblemDetails`

## Done when

- `POST` with `Bánh ngọt` → slug `banh-ngot`; a second with the same name → `banh-ngot-2`
- `POST` with `Món Bắc` → `mon-bac` (not `m-n-b-c` — the ICU trap, see CLAUDE.md § Slugs)
- blank `name` → **400 `ValidationProblemDetails`**, not a 500 and not a raw exception
- `name` over 120 chars → 400
- `GET /api/v1/categories` returns Active only, ordered by `display_order`
- flip one to InActive via PUT → disappears from the customer list, still present in
  `GET /api/v1/admins/categories`
- `DELETE` a category referenced by a course → **409**, message mentions InActive
- `DELETE` an unreferenced one → 204; it then vanishes from both lists **and its slug is
  reusable** (the partial unique index allows it)
- `status=InActive` filter on the admin list returns only those
- a customer token on any `/admins/*` route → **401**
- no token on any `/admins/*` route → 401
