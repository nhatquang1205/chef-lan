# Plan 05: free courses (price 0) are granted when the order is created

Why: Chef Lan wants some courses at 0 ₫. The customer goes through the same "Buy now → contact Chef Lan" step, the order is
created and access is granted at once, with no approval by staff.

## Decisions (confirmed)

| | |
|---|---|
| Free | A course whose **list price** is 0. A course reduced to 0 by shared-module discounts stays "not for sale" as today (staff grant by hand) |
| Flow | `POST /api/v1/orders` (`ContactChef`) for a free course: the order is created **Paid** in the same request, access granted, the response says `status: "Paid"` |
| Record | `amount_vnd = 0`, `paid_via = Free` (new value 3), no reason, no staff, no transaction. Enrollment source `OnlineCheckout`, amount paid 0 |
| Lessons | Unchanged: a lesson priced 0 is still not for sale |
| Staff orders | Unchanged: total must stay > 0 (a free gift keeps using the manual grant) |
| Display | The website shows "Miễn phí" instead of "0 ₫" (web plan `chef-lan-web/docs/plans/07-free-courses-and-owned-label.md`) |

## Build

1. `Domain/Enums/OrderPaidVia.cs`: `Free = 3` ("A free course: paid when created, nothing to collect"). Never renumber.
2. `OrderConfiguration`: `ck_orders_paid_via` → `paid_via IS NULL OR paid_via IN (1, 2, 3)`; replace `ck_orders_amount_positive`
   with `ck_orders_amount_non_negative` (`amount_vnd >= 0`). Update the column comment of `paid_via`. Migration
   `AllowFreeOrders` (`make migration NAME=AllowFreeOrders`, `make migrate`). Additive for the old release: an old image never
   writes 0 or 3.
3. `OrderService.BuildItemAsync` (course branch): when `course.PriceVnd == 0` the item is valid at 0 (list 0, discount 0,
   unit 0). Any other final price `<= 0` keeps returning `NotForSale`. The lesson branch is unchanged.
4. `OrderService.CreateAsync`: when the item's unit price is 0, skip the pending-order lookup; inside
   `IUnitOfWork.InTransactionAsync` add the order and call `OrderFulfilment.MarkPaidAsync(order, OrderPaidVia.Free, null, null,
   null, ct)` (each grant saves on its own, so the transaction keeps it all-or-nothing, as for staff orders). Return the Paid
   order. The `BankTransfer`-disabled check stays first (the web only sends `ContactChef`).
5. `OrderFulfilment.MarkPaidAsync`: `PaidReason` stays null for `Free`; nothing else changes. Check that nothing else assumes
   `amount_vnd > 0` (order list filters, payment matching: a free order never has a transaction).
6. Staff create (`CreateByStaffAsync`): keep refusing a total of 0 with its existing error (verify it still does once the
   check constraint is relaxed).
7. Tests in `tests/ChefLan.Logic.Tests`: a free course order is Paid with `PaidVia.Free` and grants; a course discounted to 0
   is still `NotForSale`; asking again for a free course already owned is 409.
8. Docs: plan table in `../spec.md`; CLAUDE.md "Payments": one line.

## Admin (`chef-lan-admin`, plan `docs/plans/21-free-orders.md`)

`OrderPaidVia` gains `'Free'`; the order dialog shows "Khóa học miễn phí" for it (today: Webhook → "Tự động qua cổng thanh
toán", else "Nhân viên duyệt"); an order list amount of 0 shows as "0 ₫" (no change if `formatVnd` already does).

## Done when

Build `-warnaserror`, tests and `make migrate` clean; admin `pnpm lint` / `pnpm build` clean. Local API: set a course to 0,
a customer `POST /api/v1/orders` `{courseSlug, method:"ContactChef"}` → 201 with `status: "Paid"`, `amountVnd: 0`; the course
plays for that customer; a second request → 409; the order shows in `GET /api/v1/admins/orders` as Paid. Data restored after.

## Implementation notes

Done. Migration `AllowFreeOrders` only drops and re-adds `ck_orders_paid_via` and swaps `ck_orders_amount_positive` for
`ck_orders_amount_non_negative`, plus the `paid_via` column comment. A free order skips the pending-order lookup, is created Paid in
one transaction (`CreateFreeAsync`) and keeps the requested payment method with `payment: null`. A race where the customer already owns
the course rolls back and answers 409 `enrollment.already_active`. Test harness courses now take a price from `FakeWorld.CoursePrice`.
