# Plan 03: orders created by staff

Why: a customer who messages the fanpage gets access from staff by hand today, with no order and no money record. Staff now
create the order in the admin, for one or more courses, and the access is granted at once. Lessons stay on the manual grant.

## Decisions (confirmed)

| | |
|---|---|
| Items | Courses only, one or more per order, each course once. Shared modules can be picked like any course |
| Customer | An existing customer. A new one is created on the Customers screen first |
| State | Paid when created: `PaidVia = Manual`, `PaidByStaffId`, a required note as `PaidReason`. There is no Pending step and no expiry |
| Method | New `OrderPaymentMethod.StaffCreated = 3` ("Admin tạo"). The check constraint widens to `(1, 2, 3)` |
| Price | The server quotes, staff may change each course's price and add a discount on the whole order with a reason. The server recomputes on create and never trusts a client total |
| Shared modules | Charged once (see the rule below) |
| Already owned | A course the customer already has (any active path) cannot be added: 409 `order.already_owned` naming it |
| Total | Must be > 0 (existing `ck_orders_amount_positive`). A free gift keeps using the manual grant |

### The shared module rule

For each shared module C included by at least one selected course:

1. If C itself is selected, or the customer already has C (the same "already has" as `QuoteCourseAsync`), every selected course that
   includes C gets its own `discount_vnd` for C.
2. Otherwise C is charged once: every selected course that includes C gets its discount except the one with the **smallest** discount,
   which pays in full. Ties: the cheaper course pays in full, then the lower id. The result never depends on the order of selection.

A course's discounts never take its price below 0, as today. Each discount carries its reason (e.g. "Đã có Nuôi men" or "Nuôi men đã tính
trong Bánh panettone"), joined into the item's `DiscountReason`.

### Money on the rows

- `orders` gains `discount_vnd numeric(12,0) NULL` and `discount_reason varchar(300) NULL` (the whole-order discount). Additive.
- Per item: `ListPriceVnd` = the course price; `UnitPriceVnd` = what this course really costs after everything, including its share of
  the order discount; `DiscountVnd` = `ListPriceVnd - UnitPriceVnd`; `DiscountReason` = the shared-module reasons, plus "Giá chỉnh tay"
  when staff changed the price, plus the order discount reason when a share of it landed on this item.
- The order discount is split across items in proportion to their price after the staff edits, in whole dong, the remainder going to the
  most expensive item. Sum of `UnitPriceVnd` = `AmountVnd`, always.
- `OrderFulfilment` records each enrollment's `AmountPaidVnd` from its item's `UnitPriceVnd` when the order has more than one item. Today it
  records the whole received amount, which is right only for a single item. Single-item orders (the web checkout) keep today's behaviour.

## API

- `POST /api/v1/admins/orders/quote` `{ customerId, courseIds[] }` → `{ items: [{ courseId, title, slug, listPriceVnd, discounts:
  [{ includedCourseId, title, amountVnd, reason }], systemPriceVnd }], subtotalVnd }`. Nothing is saved. 409 for an owned course,
  404 for an unknown or deleted one. Drafts are allowed (staff sell before publishing); the response says `status`.
- `POST /api/v1/admins/orders` `{ customerId, items: [{ courseId, unitPriceVnd? }], orderDiscountVnd?, orderDiscountReason?, note }`
  → `AdminOrderResponse` (gains `discountVnd`, `discountReason`). `unitPriceVnd` absent = the system price. Validation: whole dong, ≥ 0,
  at most the list price; order discount ≤ the sum after edits and requires a reason; `note` required, max 300; at least one item, no
  duplicates, at most 20. Customer missing or inactive: the existing enrollment errors.
- Everything in one `SaveChanges`: the order, its items and every grant. A failed grant leaves nothing behind.

## Build

1. Domain/DB: the enum value, the two order columns, the widened check, migration `AddStaffOrders`.
2. Logic: a pure `OrderQuote` calculator in `Courses/Payments/` for the shared-module rule and the split, with unit tests for: two parents
   sharing C with different discounts; three parents; C selected too; customer already owns C; a discount bigger than the course price;
   ties; the split remainder; staff edits plus an order discount.
3. `OrderService.QuoteForStaffAsync` and `CreateByStaffAsync`; `OrderFulfilment` per-item amount. Validators. `AdminOrdersController`.
4. Mapping: `discountVnd`, `discountReason` on `AdminOrderResponse`; items already carry list, discount and unit prices.
5. Docs: `../spec.md` and the Payments section of `CLAUDE.md`.

## Done when

Build and tests pass. Against the local API: the quote matches the rule for each case above with real includes; create grants every
course in one go and the customer's `/me/courses` shows them; enrollments record list price, discount, reason and their own amount; the
order shows in `GET /admins/orders` and in the customer's `/me/orders`; an owned course is a 409 and nothing is saved; a total of 0 is a
400. Test data removed. Notes added here.
