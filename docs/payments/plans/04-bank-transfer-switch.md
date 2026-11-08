# Plan 04: switch bank transfer off

Why: for this release customers buy only by contacting Chef Lan. Bank transfer must be off in the API too, and easy to turn
back on.

## Decisions (confirmed)

| | |
|---|---|
| Flag | `Payment:BankTransferEnabled`, default `false` in `appsettings.json`. On again with `Payment__BankTransferEnabled=true` |
| Effect | `/checkout/config` answers `enabled: false`; `POST /api/v1/orders` with `BankTransfer` is 503 `CheckoutDisabled` (as when SePay is not configured). Contact and staff orders unchanged |
| IPN | Still accepted whenever SePay is configured, so a transfer order created before the switch and paid after it is recorded and paid |
| Web | No change: the buy dialog already hides the transfer option when the config says disabled |

## Build

1. `PaymentOptions` (`Infrastructure/Services/Payments/PaymentOptions.cs`): `bool BankTransferEnabled { get; set; }`;
   `SePayConfigured` = today's `CheckoutEnabled` checks; `CheckoutEnabled => SePayConfigured && BankTransferEnabled`.
2. `IPaymentSettings`: add `SePayConfigured`. `PaymentsController.SePayIpn` uses it instead of `CheckoutEnabled`.
   `OrderService` keeps `CheckoutEnabled` for the config and for creating a transfer order.
3. `appsettings.json`: `"BankTransferEnabled": false` under `Payment`.
4. Docs: `deploy/README.md` (payments env vars: one line), `../spec.md` plan table, CLAUDE.md "Payments" (one line).

## Done when

Build clean. Local: `GET /api/v1/checkout/config` → `{"enabled":false}`; a customer `POST /api/v1/orders` with
`"method":"BankTransfer"` → 503; with `"ContactChef"` → 201. Add implementation notes here.

## Implementation notes

- `PaymentOptions.SePayConfigured` holds the old `CheckoutEnabled` checks; `CheckoutEnabled` is now `SePayConfigured && BankTransferEnabled`.
  `PaymentsController.SePayIpn` checks `SePayConfigured`; `OrderService` is unchanged (still `CheckoutEnabled`).
- `deploy/README.md` lists `Payment__BankTransferEnabled` with the other payment variables.
- No migration.
