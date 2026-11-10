# Plan 01: province, profile completion API, required email, drop facebook_name

Context and decisions: `../spec.md`.

## Backend (`chef-lan`): `docs/customer-profile/spec.md` + `plans/01-...`

1. **Provinces:** `ChefLan.Commons/Geography/VietnamProvinces.cs` (no domain noun, used by more than one side): the 34
   entries `(Code, Name)` in display order, `IsValid(code)`. The agent checks the list against an official 2025 source.
   `GET /api/v1/provinces` (anonymous, `Cache-Control: public, max-age=86400`).
2. **Schema:** `Customer.ProvinceCode` (`varchar(50)`, null, column comment), remove `Customer.FacebookName`. Migration
   `CustomerProvinceDropFacebookName` (`make migration`, `make migrate`, local DB only).
3. **Admin contracts** (`Logic/Customers/Contracts/CustomerAdminContracts.cs`, `CustomerAdminService`, `CustomerMappings`,
   `CustomerRepository`): drop `FacebookName` from request, list item and detail; add `ProvinceCode` to request and detail
   (and the list item, for a column). Validator: `FullName` required (≤200), **`Email` required** (valid, ≤254),
   `ProvinceCode` optional but must be a known code. The create-with-invite flow (`sendInvite`, `InviteError`) is reused
   unchanged by the CSV import; a duplicate email keeps its existing 409 code.
4. **Customer self-service:** `CustomerResponse` (`Logic/Customers/Contracts/CustomerContracts.cs`, used by `/me` and in every
   `SessionResponse`) gains `ProvinceCode` and `ProfileComplete` (phone and province both set). New
   `PUT /api/v1/me/profile` (customer only) `{ fullName, phone, provinceCode }`: all required; phone digits with optional
   leading `+`, 9–15 digits after removing spaces/dots; returns the updated `CustomerResponse`.
5. Self-registration (`CustomerRegisterRequest`) is unchanged (phone optional there); the website's gate asks for the rest.
6. Tests in `tests/ChefLan.Logic.Tests`: province validation, profile validator, `ProfileComplete`.
7. Docs: CLAUDE.md "Customers without credentials" (email now required in the admin; rows without email still exist from
   before) and "Customer invitations" (CSV import uses the same flow).

## Done when

Build `-warnaserror`, tests and `make migrate` (local) clean; migration reviewed. Add implementation notes here.

## Implementation notes

- Province list: the 34 units of Resolution 202/2025/QH15 (28 provinces, 6 cities), cross-checked against the resolution text
  on luatvietnam.vn. Display names: cities `TP. X`, provinces plain; order is Vietnamese alphabetical ignoring the `TP.` prefix.
  Codes are ASCII slugs (`ha-noi`, `ho-chi-minh`, `hue`, `dak-lak`); never rename one.
- `GET /api/v1/provinces` -> `[{ "code", "name" }]`, `Cache-Control: public, max-age=86400` (`ProvincesController`).
- `CustomerResponse` = `{ id, email, fullName, phone, provinceCode, profileComplete }`.
- `PUT /api/v1/me/profile` (`MyProfileController`, `CustomerOnly`) `{ fullName, phone, provinceCode }`: name required <=200; phone
  required, spaces and dots stripped then `^\+?[0-9]{9,15}$` (stored stripped); province must be a known code. 400
  `ValidationProblemDetails` otherwise.
- Admin request `{ fullName, email, phone?, provinceCode?, facebookProfileUrl?, note?, sendInvite }`; list item and detail carry
  `provinceCode` instead of `facebookName`. The customer search no longer matches a Facebook name.
- Migration `CustomerProvinceDropFacebookName`: Up drops `facebook_name`, adds `province_code varchar(50) null`; Down reverses
  (re-adds `facebook_name varchar(200) null`, data not restored). Tests: `tests/ChefLan.Logic.Tests/CustomerProfileTests.cs`.
