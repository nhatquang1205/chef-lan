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
