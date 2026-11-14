# Plan 10: course images through Cloudflare (img.bonbocon.com) instead of straight from B2

Why: images are WebP and small (100–440 KB), but they are served straight from the B2 bucket in `us-east-005`. Measured from
Vietnam on 2026-11-15 (Panettone's 6 gallery images): 1–3.5 s before the first byte, 2.5–6 s per image. B2 sends
`Cache-Control: public, max-age=31536000, immutable`, so a CDN can keep them at an edge near the visitor.

## Decisions (confirmed)

| | |
|---|---|
| CDN | **Cloudflare**, already in front of every bonbocon.com host. Backblaze's documented setup: proxied CNAME to the bucket's `f005` host plus a URL Rewrite rule; no egress fee from B2 to Cloudflare |
| Host | `img.bonbocon.com` (CNAME `img` -> `f005.backblazeb2.com`, proxied). Not the VPS: nginx and the Cloudflare-only snippet are not involved |
| New uploads | `Storage__B2__PublicBaseUrl=https://img.bonbocon.com` (the option already exists; `DeleteByUrlAsync` uses the same base) |
| Stored URLs | Rewritten once with `deploy/sql/rewrite-image-base-url.sql` (every text column in `courses` and `customers`, so markdown with images is covered too). No code change, no migration. The old URLs keep working, so the order of steps does not matter |
| Slider | Separate web change (chef-lan-web plan 12): the gallery images load right away instead of when swiped to |

## Steps (the user, in the Cloudflare dashboard and on the VPS)

1. Cloudflare, zone bonbocon.com, **DNS -> Records -> Add record**: type `CNAME`, name `img`, target `f005.backblazeb2.com`,
   Proxy status **Proxied** (orange). SSL/TLS stays Full (strict).
2. **Rules -> Overview -> Create rule -> URL Rewrite Rule**: name `img -> B2 bucket`; "Custom filter expression":
   Hostname equals `img.bonbocon.com`; Path -> Rewrite to -> **Dynamic**: `concat("/file/chef-lan", http.request.uri.path)`;
   query unchanged; Deploy. (Required: without it the host could serve any public B2 bucket.)
3. Check (Claude can do this part): `curl -sI https://img.bonbocon.com/images/2026/11/9a3e5e8fc041474f9b3365ffb9540fc0.webp`
   is `200`, `content-type: image/webp`; a second request shows `cf-cache-status: HIT`; `https://img.bonbocon.com/file/other-bucket/x`
   is a 404 from our bucket, not another bucket's file.
4. VPS `~/chef-lan/.env`: add `Storage__B2__PublicBaseUrl=https://img.bonbocon.com`, then
   `cd ~/chef-lan && IMAGE_TAG=$(cat .last-good-tag) docker compose -f docker-compose.prod.yml up -d` (or the next deploy). Upload one image in the admin: the returned URL starts with `https://img.bonbocon.com/`.
5. Copy `deploy/sql/rewrite-image-base-url.sql` to the VPS and run it once as a dry run (prints the changed columns and row
   counts, rolls back), then with `-v commit=1`:
   ```
   docker exec -i postgres sh -c 'psql -U "$POSTGRES_USER" -d cheflan -v ON_ERROR_STOP=1' < rewrite-image-base-url.sql
   docker exec -i postgres sh -c 'psql -U "$POSTGRES_USER" -d cheflan -v ON_ERROR_STOP=1 -v commit=1' < rewrite-image-base-url.sql
   ```
   Undo: the same with `-v old_base=https://img.bonbocon.com/ -v new_base=https://chef-lan.s3.us-east-005.backblazeb2.com/`.
6. The website pages are rendered per request, so the new URLs show at once.

## Done when

The SQL is tested on the local DB (dry run rolls back, commit applies, the reverse run restores). After the user's steps:
`img.bonbocon.com` answers with a cache HIT, `/api/v1/courses/panettone` returns `img.bonbocon.com` URLs, and the same 6 images
are measured again.

## Implementation notes

- `deploy/sql/rewrite-image-base-url.sql`: psql script, bases overridable with `-v`, loops over every `text`/`varchar` column of
  base tables in `courses` and `customers`, `replace()` only where the old base occurs, a NOTICE per changed column, rolls back
  unless `-v commit=1`. Tested on the local DB with `picsum.photos` as the old base: dry run changed 2 columns (12 rows) and rolled
  back; commit then reverse commit restored all 11 course thumbnails.
- 2026-11-15, after the user's Cloudflare steps: `img.bonbocon.com` answers 200 `image/webp`, first request `MISS`, then `HIT`
  (edge SIN); `/file/other-bucket/x` is 404. Panettone's 6 images through the CDN: MISS 1.5–3.1 s each, HIT 0.3–0.7 s each
  (straight from B2 before: 2.5–6 s). Stored URL rewrite on production: pending (the user runs the SQL).
