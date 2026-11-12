# Online payment (SePay Payment Gateway, bank transfer by QR) — spec

A customer buys a course, or an add-on lesson, on the website and pays on **SePay's hosted payment page** (bank transfer by
QR). SePay tells the API when the payment is captured; the API matches it to the order and grants access with no human
involved. Staff only deal with what the matching could not decide. This is SePay's **Payment Gateway** product
(`my.sepay.vn/pg`), not its bank-balance webhook. Card and wallet gateways (VNPay) are out of scope.

## Decisions

| | |
|---|---|
| Provider | **SePay Payment Gateway**: the browser POSTs a signed form to `https://pay.sepay.vn/v1/checkout/init`; SePay's IPN calls `POST /api/v1/payments/sepay/ipn`, authenticated by the IPN secret in the `X-Secret-Key` header |
| Payment | Customer-owned **order**; its **code is the invoice number** (`order_invoice_number`) SePay echoes back. The receiving bank account is configured in SePay, not here |
| Price | Fixed on the server when the order is created: course = `QuoteCourseAsync` (list minus included-course discount); lesson = its own `priceVnd`. Never from the client. The form is signed, so the amount cannot be changed in the browser |
| Who | Customers only. Staff accounts have no customer id and cannot buy |
| Fulfilment | One method (`OrderFulfilment.MarkPaidAsync`) for the webhook and both staff paths. It sets the order Paid and calls `GrantCourseAsync` / `GrantLessonAsync` (source `OnlineCheckout`) in one SaveChanges |
| Realtime | The website **polls** `GET /orders/{code}` (3 s, then 8 s). No SignalR: one event per purchase, and a status endpoint is needed as the fallback anyway |
| Where it lives | Courses domain, `courses` schema: it grants enrollments and points at customers, courses and lessons, so no cross-domain seam |
| Contact orders | A customer may instead choose **ContactChef** (`orders.payment_method` = 2; 1 = BankTransfer, the default). It needs no SePay, so it works while checkout is off; the order has no `payment` form, lives `Payment:ContactOrderLifetimeDays` (7), and staff approve it with `POST orders/{id}/mark-paid` (also after it expired). Asking again for the same item with the other method keeps the order and its code and resets `expiresAt` |
| Off switch | Without `Payment:BankCode/AccountNumber/AccountName` and `Payment:SePay:IpnApiKey`, checkout is off (`GET /checkout/config` says so) and the site keeps "contact chef Lan" |

## Data (migration `AddOrdersAndPayments`, additive)

- `courses.orders`: `code` (unique), `customer_id`, `status` (1 Pending, 2 Paid, 3 Expired, 4 Cancelled), `amount_vnd`,
  `expires_at`, `paid_at`, `paid_via` (1 Webhook, 2 Manual), `paid_reason`, `paid_by_staff_id` (no FK), `payment_transaction_id`
  (no FK: the two tables point at each other), `xmin` row version. CHECKs: amount > 0, Paid has a date, Manual has a reason.
- `courses.order_items`: kind (1 Course, 2 Lesson), course or lesson id, title / course slug / list price / discount / unit
  price snapshots. CHECK: exactly one target, `unit = list - discount`. One item per order in v1.
- `courses.payment_transactions`: **(provider, provider_transaction_id) unique** = idempotency key, amount, note, code found,
  raw body (`text`), nullable `order_id`, `match_status`, a note.

## Order codes and the signed form

Code = `BB` + 6 characters from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`. It is sent as `order_invoice_number` (and in the description), and
SePay returns it in the notification, so the order is found by **exact invoice number**. If it is missing, the description is
searched for a code (squashed to A-Z/0-9, `OrderCodes.FindCandidates`); every candidate is tried until one names an order.

`PaymentInstructions.For` builds the form: `merchant`, `currency=VND`, `order_amount`, `operation=PURCHASE`, `order_description`,
`order_invoice_number`, `success_url` / `error_url` / `cancel_url` (all `{site}/checkout/{code}?result=...`),
and (no `payment_method`: SePay fills it in when the merchant has one method enabled, else the customer chooses on its page), and `signature` = Base64 of HMAC-SHA256 over `name=value` pairs joined by commas, in SePay's fixed order
(`order_amount, merchant, currency, operation, order_description, order_invoice_number, customer_id, payment_method, success_url,
error_url, cancel_url`), skipping empty values. The merchant **Secret Key** signs it and never leaves the server. The test pins the
algorithm with a signature computed outside .NET.

## Reconciliation (`PaymentMatcher`, pure)

| Situation | Match status | Order |
|---|---|---|
| Invoice number names an order, Pending, amount equal | Matched | Paid |
| Amount higher | Overpaid | Paid; refund the difference by hand |
| Amount lower | Underpaid | stays Pending; staff decide |
| Order Expired or Cancelled | LateAfterExpiry | not granted; staff link or refund |
| Order already Paid | Duplicate | unchanged; refund |
| No invoice number, or an unknown one | Unmatched | none |
| A void (`TRANSACTION_VOID`), a payment not approved or captured, or a non-positive amount | Ignored | none |

The transaction row is stored **first** (its own SaveChanges), then matched, so a failure while matching never loses a
notification. A retry of the same notification returns the stored outcome; one still Unmatched is matched again (it may have
arrived before its order was visible). The customer already owning the item (a race with a staff grant) pays the order and
flags the transaction Duplicate. Expiry is **lazy**: a Pending order past `expires_at` reads as Expired.

Staff can also create an order themselves (`POST orders/quote`, `POST orders`, `PaymentMethod = StaffCreated`): Paid at once, one or more
courses, shared modules charged once (`OrderQuote`, pure), staff may edit each price and discount the whole order; see plan 03.

Staff tools (`/api/v1/admins`): `GET orders?status=&paymentMethod=&customerId=&q=&page=&pageSize=` (`status` is the effective status, so Expired includes Pending past its deadline; `q` matches code, customer name, email, phone), `GET payments?status=flagged|all|<status>`, `POST orders/{id}/mark-paid`
(reason required, optional transaction), `POST payments/{id}/link` (attach a transaction to an order and pay it),
`POST orders/{id}/cancel`. How an order was paid is on the order itself; there is no separate event table.

## The notification endpoint

`POST /api/v1/payments/sepay/ipn`. SePay sends the **IPN secret** (set on the IPN tab of the Payment Gateway, independent of the merchant
Secret Key) in `X-Secret-Key`; `Authorization: Apikey <secret>` and `X-Api-Key` are also accepted. The comparison is constant-time. 401 on a
wrong secret; 503 when payment is not configured. Answers `200 {"success": true}` for anything it stored, matched or not, so SePay does not
retry forever; a transient failure is a 5xx. The body is `{timestamp, notification_type: ORDER_PAID | TRANSACTION_VOID, order{...,
order_invoice_number, order_amount}, transaction{transaction_id, transaction_amount, transaction_status, transaction_date,...}, customer{...}}`.
**`SePayIpnParser` is the only code that knows these names.** Source: SePay's IPN page, whose transaction object is only partly documented:
compare with a real notification (SePay dashboard, "Nhật ký IPN") before relying on it. An unreadable body is stored for staff, never guessed at.
The transaction row's `account_number` column exists but the Gateway does not send one.

## Configuration

`Payment:SePay:MerchantId` ("Mã đơn vị"), `Payment:SePay:SecretKey` (merchant Secret Key, signs the form), `Payment:SePay:IpnSecret` (IPN tab),
`Payment:SePay:CheckoutUrl` (default production), `Payment:CodePrefix` (default `BB`), `Payment:OrderLifetimeMinutes` (default 1440). The website's
origin comes from `Web:PublicBaseUrl` (which the mails already need). Secrets only as environment variables (`Payment__SePay__SecretKey`).
Checkout is off until MerchantId, both secrets and `Web:PublicBaseUrl` are set. nginx must pass `/api/v1/payments/` through (server to server,
no CORS). The sandbox address is not in SePay's pages we read: take it from their Sandbox page and set `CheckoutUrl` with the sandbox merchant.

## Deferred (add only if real use shows the need)

A timer job that re-matches Unmatched/LateAfterExpiry transactions; a daily gap check against SePay's order API (`GET https://pgapi.sepay.vn/v1/order/detail/{id}`, needs API credentials); a
daily report of paid orders vs matched transactions; a refund action (staff revoke access with the existing revoke endpoints
and refund the money by hand); invoices (hóa đơn điện tử).

## Plans

| # | Repo | Plan | Status |
|---|---|---|---|
| B1 | chef-lan | [Schema, orders, reconciliation, IPN, admin API](plans/01-orders-and-ipn.md) | done |
| B2 | chef-lan | [Contact orders and the admin order list](plans/02-contact-orders-and-admin-list.md) | done |
| B3 | chef-lan | [Orders created by staff](plans/03-staff-orders.md) | done |
| B4 | chef-lan | [Switch bank transfer off](plans/04-bank-transfer-switch.md) | done |
| B5 | chef-lan | [Free courses granted on order](plans/05-free-courses.md) | done |
| B6 | chef-lan | [Discord message for a new contact order](plans/06-contact-order-discord.md) | done (needs the webhook in the VPS `.env`) |
| A2 | chef-lan-admin | `docs/plans/20-create-order.md` | done |
| A3 | chef-lan-admin | `docs/plans/21-free-orders.md` | done |
| W | chef-lan-web | `docs/plans/` checkout page + buy button | see that repo |
