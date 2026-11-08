# Plan 01: catalog state API

Context: `../spec.md`.

## Build

1. **One discount rule.** Move the capping loop of `EnrollmentService.BuildQuoteAsync`
   (`src/ChefLan.Logic/Courses/Services/EnrollmentService.cs`, the `foreach` over include rows the customer has) into a pure
   `internal static` helper in `ChefLan.Logic/Courses/` (for example `Payments/IncludeDiscounts.cs`):
   input the list price, the include rows (in the order `ListForCourseAsync` returns them) and the set of owned course ids;
   output the discounts (`QuoteDiscountResponse`) and the final price. `BuildQuoteAsync` calls it; behaviour unchanged.
2. `IEnrollmentRepository.ListOwnedCourseIdsAsync(customerId, utcNow)`: every course owned whole. The same two queries as
   `OwnedCourseIdsAsync` (`Infrastructure/Repositories/Courses/EnrollmentRepository.cs`) without the id filter: direct active
   unexpired enrollments, plus `course_includes.included_course_id` of those courses. Returns a set.
3. `ICourseIncludeRepository.ListByIncludedCoursesAsync(IReadOnlyCollection<long> includedCourseIds)`: the include rows whose
   included course is in the set and whose parent course is published and not deleted, with `IncludedCourse` and `Course`
   loaded (title and price needed), ordered as `ListForCourseAsync` orders them.
4. `MyCatalogStateResponse(IReadOnlyList<long> OwnedCourseIds, IReadOnlyList<MyCoursePriceResponse> Prices)` and
   `MyCoursePriceResponse(long CourseId, decimal ListPriceVnd, decimal FinalPriceVnd)` in `EnrollmentResponses.cs`.
5. `IEnrollmentService.GetMyCatalogStateAsync(customerId)`: owned set (2); include rows of owned courses (3); drop parents that
   are owned; group by parent; run the helper (1) with the parent's `PriceVnd`; keep only `FinalPriceVnd < ListPriceVnd`.
   Owned ids sorted ascending.
6. `MyCoursesController`: `[HttpGet("~/api/v1/me/catalog-state")]`, same `CustomerOnly` policy, `TryGetCustomerId` guard
   (401), sets `Cache-Control: private, no-store`, XML doc comment like the existing action.
7. Tests in `tests/ChefLan.Logic.Tests`: the helper (no owned module, one module, two modules capped at the list price, a
   zero discount ignored).
8. Docs: status in `../spec.md`; CLAUDE.md, section "Lesson prices and access": one line about the endpoint and the shared rule.

## Done when

`dotnet build ChefLan.slnx -c Release -warnaserror` and `dotnet test tests/ChefLan.Logic.Tests` clean. Against the local API
(local DB only): a customer owning shared module C (included by P with discount 300k) gets C in `ownedCourseIds` and P at
`price - 300k`, the same as `GET /api/v1/courses/{P}/access` / the order quote; owning P lists P and C, no prices for them;
a staff token and no token answer 401. Test data removed afterwards. Add implementation notes here.

## Implementation notes

- The shared rule is `IncludeDiscounts.Apply(listPriceVnd, includes, ownedCourseIds)` in `ChefLan.Logic/Courses/Payments/`
  (`internal static`), returning `(IReadOnlyList<QuoteDiscountResponse> Discounts, decimal FinalPriceVnd)`. `BuildQuoteAsync`
  and `GetMyCatalogStateAsync` both call it. Tests: `IncludeDiscountsTests`.
- `ListByIncludedCoursesAsync` orders by parent, then sort order, then included course id, so each parent's group has the
  same order `ListForCourseAsync` gives.
- Staff and anonymous get 401 from the `CustomerOnly` policy. Authenticated checks are done by hand against the local API.
