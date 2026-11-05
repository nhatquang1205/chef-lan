# B1 — Orders, reconciliation, notification endpoint, admin API

Read `../spec.md`.

## What was built

- Domain: `Order`, `OrderItem`, `PaymentTransaction`; enums `OrderStatus`, `OrderPaidVia`, `OrderItemKind`, `PaymentMatchStatus`.
- DB: three configurations and the migration `AddOrdersAndPayments`.
- Logic (`Courses/`): `IOrderService` / `OrderService`, `IPaymentService` / `PaymentService`, `OrderFulfilment`, repositories
  ports, `Payments/{OrderCodes,PaymentMatcher,SePayIpnParser,PaymentInstructions}` (the last builds the signed SePay form), validators, errors, mappings.
- Infrastructure: `OrderRepository`, `PaymentTransactionRepository`, `PaymentOptions` (the `IPaymentSettings` adapter).
- API: `CheckoutController` (config), `OrdersController` (customer), `PaymentsController` (the notification), `AdminOrdersController`.
  Rate limits `orders-create` (10/min) and `payments-ipn` (120/min).
- Tests: `tests/ChefLan.Logic.Tests` (rules, parser, and the payment flow over in-memory fakes).

## Done when

Checked against a scratch Postgres database (dropped afterwards), with Gateway-shaped notifications: no secret or a wrong one is 401 (`X-Secret-Key`, and the
`Apikey` form also works); the same notification twice leaves one row; an order is created with a signed form and asking again returns it; an
underpayment leaves it Pending; a `TRANSACTION_VOID` is ignored, the exact amount pays it, creates an `OnlineCheckout`
enrollment and `GET /me/courses` lists it; buying again is 409; a second transfer is Duplicate; staff list flagged
payments, link a transaction (order Paid, `paid_via = Manual`, reason and staff id recorded), and a mark-paid without a reason is 400.
`dotnet build` clean, 53 tests pass.

Reworked from an earlier design built on SePay's bank-balance webhook (own QR, code searched in the transfer note) after the shop turned out to use the Payment Gateway.

## Not covered by tests

Database behaviour (the unique transaction key under a real race, `xmin` conflicts between a webhook and a staff mark-paid)
needs Testcontainers.PostgreSql; the service tests use fakes. The race path is simulated, not exercised.
