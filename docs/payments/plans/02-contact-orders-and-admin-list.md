# Plan 02: contact orders and the admin order list

Why: "Mua ngay" now offers two ways to pay: contact chef Lan directly, or bank transfer. Contacting her must still
create an order, and staff approve it in the admin. The admin needs to list orders, also per customer. Refunds are out.

## Decisions

| | |
|---|---|
| Method | New `orders.payment_method`: 1 BankTransfer, 2 ContactChef. Existing rows and the default are 1, so the migration is additive and the previous image keeps working |
| Checkout off | Only BankTransfer needs SePay. A ContactChef order works when `CheckoutEnabled` is false |
| Contact order lifetime | 7 days, `Payment:ContactOrderLifetimeDays`. Expiry only tells the customer it is no longer held; staff can still approve an expired order |
| Approve | The existing `POST orders/{id}/mark-paid`, reason required. No new endpoint and no second path to Paid: `OrderFulfilment.MarkPaidAsync` stays the only one |
| Changing method | Same item, same price, still pending: the customer keeps the order and its code; the method and `ExpiresAt` follow the new choice |
| Contact order and SePay | `payment` is null in the response, so no form is ever shown |

## Build

1. Domain: enum `OrderPaymentMethod { BankTransfer = 1, ContactChef = 2 }`; `Order.PaymentMethod`, default BankTransfer.
2. DB: `OrderConfiguration`: int conversion, default 1, comment, `ck_orders_payment_method`. Update the "paid by bank
   transfer" wording in the entity and config summaries. `make migration NAME=AddOrderPaymentMethod`, `make migrate`.
3. Settings: `IPaymentSettings.ContactOrderLifetime`, `PaymentOptions.ContactOrderLifetimeDays` default 7, `appsettings.json`,
   the fake in `tests/ChefLan.Logic.Tests/Fakes.cs`.
4. `CreateOrderRequest` gains `OrderPaymentMethod Method = BankTransfer`; validator `IsInEnum`.
5. `OrderService.CreateAsync`: return `CheckoutDisabled` only for BankTransfer. Lifetime by method. For an `existing`
   order at the same price with another method: set the method, reset `ExpiresAt`, save, return it.
6. Mapping and contracts: `PaymentMethod` on `OrderResponse` and `AdminOrderResponse`, `CustomerPhone` on
   `AdminOrderResponse`. `Payment` only while Pending AND BankTransfer.
7. Admin list `GET /api/v1/admins/orders?status=&paymentMethod=&customerId=&q=&page=&pageSize=`:
   - A filter record in `Contracts/Requests`, passed down to `IOrderRepository.ListAsync(filter, utcNow, paging)`.
   - `status` now filters on the EFFECTIVE status, because that is what the response shows: Pending is stored Pending and
     not past `expires_at`; Expired is stored Expired or stored Pending past it. Others as stored. Fix the action's summary.
   - `q`: ILIKE on code, customer name, email, phone, inline in the repository with LIKE metacharacters escaped (project
     rule; follow `CustomerRepository.ListAsync`). The code is compared case-insensitively.
   - Newest first, as today. `CancelAsync`, the customer side and `ListMineAsync` do not change.
8. Tests in `tests/ChefLan.Logic.Tests`: contact order with checkout off; transfer with checkout off is still 503; a
   contact order has no payment and a 7-day expiry; changing method keeps the code; approving a contact order, also an
   expired one, grants access through `MarkPaidAsync`; a second approve is `AlreadyPaid`.
9. Docs: `docs/payments/spec.md` and the Payments section of `CLAUDE.md`. Remove "order history for admins by customer"
   from the deferred list.

## Not in scope

Refunds, staff notification on a new contact order, changing the amount on approval, a bank-transaction screen.

## Done when

`dotnet build ChefLan.slnx -c Release -warnaserror` and `dotnet test tests/ChefLan.Logic.Tests` pass, and against the local
API with SePay unset: a contact order is created, `/checkout/config` still says disabled, a transfer order is 503, each
admin filter works, including `status=Expired` on a backdated order, and mark-paid grants access. `q=%` must not return
everything. Test data removed afterwards. Add implementation notes here.
