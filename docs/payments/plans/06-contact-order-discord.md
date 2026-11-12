# Plan B6: a Discord message for every new contact order

Why: a customer who picks "Liên hệ Chef Lan" creates an order and then waits. Staff only see it by opening the admin, so
the answer can take hours. A message in Discord when the order is created lets Chef Lan answer at once.

## Decisions (confirmed)

| | |
|---|---|
| Event | **Only a new contact order**: `OrderService.CreateAsync` with `Method = ContactChef` that saves a new order, **or** switches an existing waiting order to `ContactChef`. Not when the same contact order is requested again (it is handed back unchanged), not bank transfer, not free courses, not staff-created orders |
| Channel | The **same Discord channel** as the Dozzle error alerts, through its own setting so it can move later |
| Content | Order code, customer (name, email, phone, province name), item (course title, or lesson title + its course), amount in VND, held until (Vietnam time), link to the admin order list filtered by the code |

## Build

1. **Port** `ChefLan.Logic/Courses/Interfaces/IOrderNotifier.cs`: `Task ContactOrderCreatedAsync(ContactOrderNotice notice, CancellationToken)`.
   `ContactOrderNotice` is a record in `Logic/Courses/Contracts` with the fields above (already resolved: names, not ids).
   The province name comes from `VietnamProvinces` (Commons).
2. **`OrderService.CreateAsync`**: after the order (new, or switched to ContactChef) is saved, build the notice and call the
   notifier. It must never change the result: wrap the call so any exception is caught and logged as **Warning** (an Error
   would make Dozzle post to the same webhook that just failed). Customer data needed for the notice comes from the
   `ICustomerDirectory` lookup the method already does; extend what that port returns only if a field is missing (no
   `Logic.Customers` reference).
3. **Adapter** `ChefLan.Infrastructure/Services/Notifications/DiscordOrderNotifier.cs`, a typed `HttpClient` (timeout **3 s**),
   POSTing `{ content, allowed_mentions: { parse: [] } }` to the webhook. `allowed_mentions` is required: a customer named
   `@everyone` must not ping the channel. Escape Discord markdown in customer-typed text (`* _ ~ \` | > \`). Keep the content
   under 2000 characters (cut long titles). A non-2xx answer is a Warning log, not an exception.
   The message text is built by a pure `internal static` formatter so it can be tested without HTTP.
4. **Options** `Notifications:Discord:WebhookUrl` and `Notifications:Discord:AdminBaseUrl` (`https://admin.bonbocon.com`).
   Empty URL -> `LoggingOrderNotifier` (logs the text at Information in Development, a one-line Information elsewhere), same
   pattern as `IEmailSender`. Empty `AdminBaseUrl` -> no link line. Both keys in `appsettings.json` as empty strings. The admin
   list reads the code from `?q=` (check `chef-lan-admin/src/features/orders/OrdersPage.tsx` `parseParams` for the exact name).
5. **Message** (Vietnamese, plain text):
   ```
   🛒 Đơn liên hệ mới **CL-XXXXXX**
   Khách: Nguyễn Văn A · a@example.com · 0901234567 · Hà Nội
   Khóa học: Macaron            (or: Bài học: <lesson> — <course>)
   Số tiền: 550.000 đ
   Giữ tới: 19/11/2026 10:30
   https://admin.bonbocon.com/orders?q=CL-XXXXXX
   ```
   Missing phone or province shows `—`. VND formatting: reuse what Commons has (`Vnd`), dots as thousands separator.
6. **Tests** (`tests/ChefLan.Logic.Tests`, existing fakes): a new contact order notifies once with the right fields; switching
   a waiting bank-transfer order to contact notifies; requesting the same contact order again does not; bank transfer and a
   free course do not; a notifier that throws still returns the order. Formatter: markdown escaped, `—` for missing fields,
   length capped.
7. **Docs**: CLAUDE.md "Payments" one line; `deploy/README.md` `.env` list gets `Notifications__Discord__WebhookUrl` (same URL as
   `~/chef-lan-monitoring/.env`) and `Notifications__Discord__AdminBaseUrl=https://admin.bonbocon.com`. This repo is public:
   never a real webhook URL in any committed file.

## Done when

`dotnet build ChefLan.slnx -c Release -warnaserror` and `dotnet test tests/ChefLan.Logic.Tests` clean. Run against the local API
with no webhook set: creating a contact order logs the message text. No real webhook is called in tests.

## Implementation notes

Built as planned. Files:

- Logic: `Courses/Interfaces/IOrderNotifier.cs`, `Courses/Contracts/ContactOrderNotice.cs`; `OrderService` takes `IOrderNotifier` and `ILogger<OrderService>` and calls `NotifyContactOrderAsync` after the save, in two places only: a new order saved with `ContactChef`, and a waiting order switched to `ContactChef`. Any exception is caught and logged as a Warning. `CustomerRef` gained optional `Phone` and `ProvinceCode` (filled by `CustomerDirectory`); the province name is resolved in Logic from `VietnamProvinces`.
- Infrastructure `Services/Notifications/`: `DiscordOptions`, `ContactOrderMessage` (pure internal static formatter), `DiscordOrderNotifier` (typed HttpClient, 3 s timeout, `allowed_mentions.parse = []`, non-2xx or network failure = Warning without the URL), `LoggingOrderNotifier`. DI in `AddTechnicalServices`: Discord notifier when `Notifications:Discord:WebhookUrl` is set, else the logging one. Both keys added to `appsettings.json` as empty strings.
- Tests: `ContactOrderMessageTests.cs` (5), five new cases in `OrderServiceTests.cs`.

Deviations:
- Infrastructure now has `InternalsVisibleTo ChefLan.Logic.Tests` and the test project references Infrastructure, so the internal formatter can be tested without making it public.
- "Held until" is UTC+7 added as a fixed offset (Vietnam has no DST), not a time-zone lookup, so it does not depend on tzdata in the container.
- For a lesson order the course title is looked up by the item's course slug (the order item stores no course title).
- Admin link is `{AdminBaseUrl}/orders?q=CODE` (`q` confirmed in `OrdersPage.tsx` `parseParams`).
- I did not run the API locally (needs secrets and a DB); the logging path is covered by the build and unit tests only.

Proposed `deploy/README.md` `.env` lines (not applied):

```
# Discord channel for new contact orders (same webhook URL as ~/chef-lan-monitoring/.env). Empty = only logged.
Notifications__Discord__WebhookUrl=
Notifications__Discord__AdminBaseUrl=https://admin.bonbocon.com
```

Proposed CLAUDE.md "Payments" line (not applied):

- **Contact-order alert:** a new contact order (or a waiting order switched to `ContactChef`) posts to Discord through `IOrderNotifier` (`DiscordOrderNotifier`; `LoggingOrderNotifier` when `Notifications:Discord:WebhookUrl` is empty). It is best effort: failures are Warnings (an Error would be re-posted to the same webhook by Dozzle) and never change the order result. The webhook URL is a credential: environment only. Plan `docs/payments/plans/06-contact-order-discord.md`.
