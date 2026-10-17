# B3 — Discount quote and recording it on grants

Needs B2. Read `../spec.md`.

## Quote

A single use case computes the price of course P for a customer:
`IEnrollmentService.QuoteCourseAsync(courseId, customerId)` →
`CourseQuoteResponse { listPriceVnd, discounts: [{ includedCourseId, title, discountVnd, reason }], finalPriceVnd }`
where a discount applies for each include (P → C) whose C the customer already has (`OwnedCourseIdsAsync`, excluding a
"has C" that comes only from P itself). `reason` is Vietnamese, e.g. "Đã có khóa Nuôi men tự nhiên". `finalPriceVnd =
max(0, list − Σ discounts)`.

- Public, signed-in customer: the access endpoint (`/courses/{slug}/access`) gains `quote` (null for anonymous/staff, and
  null when the customer already owns P).
- Admin: `GET /api/v1/admins/customers/{customerId}/quote?courseId=` → `CourseQuoteResponse`.

## Recording on grants

`GrantCourseAccessRequest` gains optional `listPriceVnd`, `discountVnd`, `discountReason` (validated: whole dong,
discount ≤ list price, reason ≤ 300). Stored on the enrollment (reactivation overwrites them). The admin sends what the
quote suggested (or what staff changed). `CourseEnrollmentResponse` and the customer access list return them.
No server-side enforcement that `amountPaidVnd` equals the final price: staff decide.

## Done when (local DB, clean up after)

P (2.000.000 ₫) includes C with discount 300.000. Customer with C → quote 1.700.000 with one reason; customer with Q
(which also includes C) → same; customer with nothing → no discount; customer already owning P → access `quote` null.
Grant P with the quoted values → stored and returned. Discount > list → 400. Release build clean. Status + notes.

## Implementation notes (done)

No migration (columns came with B1). Code: `IEnrollmentService.QuoteCourseAsync(courseId, customerId)`, shared private
`BuildQuoteAsync` in `EnrollmentService`; `GrantCourseAccessRequest` gains three fields; `AdminEnrollmentsController` gains the quote route.

**Quote** (`CourseQuoteResponse`):
```
{ listPriceVnd, discounts: [{ includedCourseId, title, discountVnd, reason }], finalPriceVnd }
```
`reason` = `"Đã có khóa {title}"`. A discount is listed for each include (any status of C) whose C the customer has (direct
enrollment, or an enrollment in another course that includes it), with `discountVnd > 0`. **Clamp:** discounts are applied in
the parent's include order and each is capped to what is left of the list price, so `finalPriceVnd >= 0` even when the stored
discount is higher than a later-lowered price (the returned `discountVnd` is the capped amount; zero-amount ones are omitted).
If the customer already has P, no discount is computed from P itself.

**Public** `GET /api/v1/courses/{slug}/access` -> `CourseAccessResponse` gains `quote: CourseQuoteResponse | null` (always
serialized; null for staff, and null when the customer already owns P directly).

**Admin** `GET /api/v1/admins/customers/{customerId}/quote?courseId=` (StaffOrAdmin) -> `CourseQuoteResponse`. 404 for an
unknown customer (`enrollment.customer_not_found`) or course (`course.not_found`). If the customer already owns P it returns
`discounts: []`, `finalPriceVnd = listPriceVnd` (a grant would be 409 anyway).

**Grant** `POST /api/v1/admins/enrollments` accepts optional `listPriceVnd`, `discountVnd`, `discountReason`. Rules (400
validation errors keyed `ListPriceVnd` / `DiscountVnd` / `DiscountReason`): both amounts whole dong (`Vnd.IsValid`);
`discountVnd` needs `listPriceVnd` and must be <= it; `discountReason` <= 300 chars and needs `listPriceVnd`. Without
`listPriceVnd` all three are stored as NULL; reactivating a revoked/expired enrollment overwrites them. `amountPaidVnd` is not
checked against the final price. Lesson grants are unchanged.

**Enrollment response**: `CourseEnrollmentResponse` (grant response and `GET /admins/customers/{id}/access` `courses[]`) gains
`listPriceVnd`, `discountVnd`, `discountReason` (all nullable). `/me/courses` is unchanged.

Verified locally (curl): P 2.000.000 includes C (300.000), Q (1.500.000) includes C (100.000): C holder and Q holder both
quote 1.700.000 for P with one reason; no-course customer 2.000.000 with no discounts; owner of P gets `quote: null` on access;
grant with 2.000.000/300.000/reason stored and returned in grant response and access list; discount > list, discount without list,
fractional list price, 301-char reason -> 400; P lowered to 200.000 -> discount capped at 200.000, final 0. Release build clean.
