# What a signed-in customer owns, for the website — spec

## Why

The home page is rendered on the server and cached for everyone, so it cannot know who is looking. A signed-in customer
still saw the courses they own, and a course including a shared module they own showed its full price (the discount was
only on the course page). The website now asks once for "what do I own and what do I pay", keeps the answer in the
browser (IndexedDB) and personalises the home page from it. See `chef-lan-web/docs/client-store.md` for the browser side.

## Decisions (confirmed)

| | |
|---|---|
| Endpoint | `GET /api/v1/me/catalog-state`, customer token only (staff and anonymous: 401). `/me/courses` is unchanged |
| Owned | Owned **whole**: an active, unexpired enrollment, or through a parent course that includes it. Owning some lessons does not count |
| Prices | Only for published courses not owned whose price is reduced by shared modules the customer owns. Same rule and same numbers as `QuoteCourseAsync` (one shared helper) |
| Caching | `Cache-Control: private, no-store`. The website refetches after login and when its copy is 30 minutes old |

## Response

```json
{
  "ownedCourseIds": [12, 15],
  "prices": [{ "courseId": 20, "listPriceVnd": 1500000, "finalPriceVnd": 1200000 }]
}
```

## Plans

| # | Repo | Plan | Status |
|---|---|---|---|
| B1 | chef-lan | [Catalog state API](plans/01-catalog-state-api.md) | done |
| W | chef-lan-web | `docs/plans/06-personalised-home.md` | see that repo |
