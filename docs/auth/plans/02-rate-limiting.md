# Plan 2 — Rate limiting policies

**Status: done.** The per-email dimension is a global limiter keyed on the policy name
from endpoint metadata, fed by a small middleware that reads `email` from the JSON body
(partitioners are synchronous). `UseForwardedHeaders` is only enabled when
`ForwardedHeaders:KnownProxies` is configured. Verified: 6th login from one IP → 429 with
`Retry-After` and `application/problem+json`; one email spread over two IPs is blocked on the
email bucket alone; `GET /api/v1/courses` is never limited.

No endpoints. See [spec.md](../spec.md).

**Goal:** define the limiter and its named policies **before** any login endpoint exists, so
login cannot ship unprotected. Plans 3 and 4 attach the policy names, and attaching them is
on their done-lists.

---

## Policies

Fixed window, partitioned by client IP **and** by the submitted email where there is one —
per-IP alone is defeated by a botnet, per-email alone by spraying many accounts from one host.

| policy | limit | applies to |
|---|---|---|
| `auth-login` | 5 / minute | both login endpoints |
| `auth-register` | 3 / minute | customer register |
| `auth-refresh` | 20 / minute | both refresh endpoints |

- Rejection → **429** with a `Retry-After` header.
- Partition key for the email dimension is the **normalized, lowercased** email, so
  `A@B.vn` and `a@b.vn` share a bucket. `citext` already makes them the same account.
- Reject responses must be `ProblemDetails`, like every other error in the API.

Relationship to lockout (plan 3/4): **lockout protects one account, rate limiting protects
the endpoint.** Both are needed — lockout alone lets an attacker spray 10,000 accounts with
one guess each, and rate limiting alone lets them grind one account from many IPs.

## Behind a proxy

`HttpContext.Connection.RemoteIpAddress` is the **proxy's** address once this is deployed
behind nginx, Cloudflare or a load balancer — so every request shares one partition and the
limiter either does nothing useful or locks everyone out together.

`UseForwardedHeaders` must be configured with `KnownProxies`/`KnownNetworks` for the IP
partition to mean anything. Configuring it **without** restricting known proxies is worse
than not doing it, because then a client can forge `X-Forwarded-For` and mint itself a fresh
partition per request, bypassing the limit entirely.

This is local-only until deployment, so it can be configured now and verified then — but it
must not be forgotten.

## Files

- `ChefLan.Courses.API/Infrastructure/RateLimitingConfiguration.cs` —
  `AddChefLanRateLimiting()` with the three named policies, `AddChefLanForwardedHeaders()`
- `ChefLan.Courses.API/Program.cs` — register, plus `app.UseRateLimiter()` after
  `UseAuthentication`/`UseAuthorization` so a 401 is not spent from the login budget
- `appsettings.json` — permit/window per policy, so they are tunable without a rebuild

## Done when

- full rebuild, 0 warnings; app boots; `GET /api/v1/courses` still 200 and **not** rate
  limited (no policy attached)
- a temporary endpoint tagged `auth-login` returns 429 on the 6th call within a minute, with
  `Retry-After` present, and a `ProblemDetails` body — then remove the temporary endpoint
- two different emails from the same IP consume **separate** email partitions but share the
  IP one
- limits are readable from configuration, not hardcoded constants
