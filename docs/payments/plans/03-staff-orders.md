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
| Already owned | A course the customer already has (any active path): the quote flags it, the create refuses it with 409 `order.already_owned` naming it |
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
  [{ includedCourseId, title, amountVnd, reason }], systemPriceVnd, alreadyOwned }], subtotalVnd }`. Nothing is saved. An owned course
  is not an error here: it comes back with `alreadyOwned: true`, no discounts and is left out of `subtotalVnd`, so the admin can mark
  that row. 404 for an unknown or deleted course. Drafts are allowed (staff sell before publishing); the response says `status`.
- The admin's course picker uses the normal admin course list, with no customer filter. Ownership is decided here, on the server: the
  quote reports it and the create refuses it.
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

Build and tests pass. Against the local API: the quote matches the rule for each case above with real includes and flags an owned course; create grants every
course in one go and the customer's `/me/courses` shows them; enrollments record list price, discount, reason and their own amount; the
order shows in `GET /admins/orders` and in the customer's `/me/orders`; an owned course is a 409 and nothing is saved; a total of 0 is a
400. Test data removed. Notes added here.

## Notes (implementation)

Done. 75 tests pass (62 before; 11 new in `OrderQuoteTests`, 2 in `OrderServiceTests`).

Changes from the plan, and why:

- **One transaction, not one SaveChanges.** `GrantCourseAsync` saves on its own, so the order and its grants cannot share a single
  `SaveChanges`. Added `IUnitOfWork.InTransactionAsync` (Infrastructure runs it inside the context's execution strategy, which the
  retrying Npgsql strategy requires); a failed grant, or a grant racing the order, rolls everything back. Not exercised live (no safe way
  to force a failing grant locally); the success path and the pre-checks were.
- **Source of the enrollments is `AdminGrant`** for `StaffCreated` orders (`OnlineCheckout` otherwise), since staff made them.
- **Reason wording:** "Đã có {module}" and "{module} đã tính trong {course}" (no word "khóa", as in the plan's examples).
- **Payer of a module is picked on the raw `discount_vnd` of each include**, before the cap at the course price.
- **The remainder of the split** goes to the most expensive item but never past that item's price (leftover moves to the next one), so no
  unit price can go negative with tiny amounts.
- Order `expires_at` is set to the creation time (column is NOT NULL; a Paid order never expires).
- `OrderItemResponse` gained `discountReason` (the reasons were not on any response). `POST /admins/orders` answers 201.
- New errors: `order.total_not_positive`, `order.discount_too_large`, `order.price_out_of_range` (400); `order.already_owned` (409, names the course).

Quote numbers on local data (modules A=41 price 550k, B=42; parents P46 800k includes A 100k and B 50k, P47 750k includes A 150k, P48 800k
includes A 150k, P49 850k includes B 900k), customer owns nothing unless noted:

| Case | Selection | Prices |
|---|---|---|
| Two parents, different discounts | 46, 47 (either order) | 46: 800000 (pays A), 47: 600000 |
| Three parents | 46, 47, 48 | 800000, 600000, 650000 |
| Module selected too | 46, 47, 41 | 700000, 600000, 550000 |
| Customer owns the module | 41 owned; 41, 46, 47 | 41 flagged owned and left out; 700000, 600000 (subtotal 1300000) |
| Tie | 47, 48 (both 150000) | 47: 750000 (cheaper pays), 48: 650000 |
| Discount bigger than the price | 49, 42 | 49: 0 (discount capped at 850000), 42: 600000 |

Create, on the same data: 46 + 47 with an order discount of 100000 gave 742857 + 557143 = 1300000 (split 57143 / 42857 + 150000 module
discount; the 1 dong remainder went to 46). Enrollments recorded list price, discount, reason and their own amount; `/me/courses` and
`/me/orders` showed both, `GET /admins/orders?paymentMethod=StaffCreated` listed it with the customer's name. Selecting an owned course
again: 409 `order.already_owned`, nothing saved. A total of 0 (price edited to 0): 400 `order.total_not_positive`.
