# B2 — Admin customers API

**Goal:** staff can list, search, create, edit, deactivate and reactivate customers. Access grants already exist
(`/admins/enrollments`, `/admins/lesson-enrollments`, `GET /admins/customers/{id}/access`); this plan does not change them.

All under `[Authorize(Policy = StaffOrAdmin)]`, controller `Controllers/Admin/AdminCustomersController.cs`, use cases in
`ChefLan.Logic/Customers/Services/CustomerAdminService.cs` (+ interface, validators, contracts, mappings) — the Customers
domain folder, **not** Courses. Repository additions in `ICustomerRepository` / `CustomerRepository`.

## Endpoints

```
GET  /api/v1/admins/customers?q=&status=&page=1&pageSize=20   -> PagedResult<CustomerAdminListItem>
GET  /api/v1/admins/customers/{id}                            -> CustomerAdminDetail
POST /api/v1/admins/customers                                 -> 201 CustomerAdminDetail
PUT  /api/v1/admins/customers/{id}                            -> CustomerAdminDetail
POST /api/v1/admins/customers/{id}/deactivate                 -> 204  (revokes all refresh tokens)
POST /api/v1/admins/customers/{id}/reactivate                 -> 204
```
(`POST /{id}/invite` comes in B3; `POST` here accepts `sendInvite` but ignores it until B3, or B3 adds it.)

- `q`: ILIKE on full_name, email, phone, facebook_name (escape LIKE metacharacters like `CourseRepository.EscapeLikePattern`).
- `status`: `Active | Invited | InviteExpired | NotInvited | Inactive` (rules in `../spec.md`). Newest first.
- Request (create and update share it): `{ fullName (required ≤200), email?, phone?, facebookName?, facebookProfileUrl?, note?, sendInvite = false }`.
  Email lowercased + trimmed, valid format, unique among non-deleted customers → 409 `customer.email_already_used`.
  `facebookProfileUrl` must be http(s). A customer with no email is allowed (Facebook-era records).
- Update: changing the email clears a pending invite (`invite_token_hash`, `invite_expires_at` → null). An **activated**
  customer's email may be changed (it is their login) — show a warning in the admin, not here.
- `CreatedByStaffId` from `ICurrentUser.TryGetStaffId`.
- List item: `{ id, fullName, email?, phone?, facebookName?, status, invitedAt?, activatedAt?, lastLoginAt?, createdAt, courseCount, lessonCount }`
  (`courseCount`/`lessonCount` = active, unexpired grants; computing them needs the courses tables — add a small read
  port in Customers, or return them from a separate query; do not reference `Logic.Courses` from `Logic.Customers`).
  If that boundary makes counts awkward, drop them from the list and show access only on the detail page — note the choice.
- Detail: list fields + `facebookProfileUrl?, note?, isActive, inviteExpiresAt?`.
- Errors: 404 `customer.not_found`, 409 `customer.email_already_used`, 400 validation.

## Done when (curl against the local DB; clean up the rows afterwards)

- Create with and without email; duplicate email (any case) → 409; list shows them, `q` finds by phone and by
  Facebook name; `status=NotInvited` filters.
- Update email on a customer with a pending invite (after B3) clears it.
- Deactivate → that customer's existing refresh token stops working and login returns `customer.account_inactive`;
  reactivate → login works again.
- Customer token on these routes → 401. Release build clean.
