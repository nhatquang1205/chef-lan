#!/usr/bin/env bash
# Runs on the VPS, piped in over ssh by .github/workflows/deploy.yml (with the compose file and
# the generated schema SQL already copied into $APP_DIR). Also fine to run by hand:
#   IMAGE_TAG=<git-sha> bash deploy-api.sh
set -euo pipefail

APP_DIR="${APP_DIR:-$HOME/chef-lan-api}"
PG_CONTAINER="${PG_CONTAINER:-postgres}"
DB_NAME="${DB_NAME:-cheflan}"
DB_USER="${DB_USER:-cheflan}"
API_PORT="${API_PORT:-5100}"
IMAGE_TAG="${IMAGE_TAG:?IMAGE_TAG is required}"
COMPOSE=(docker compose -f docker-compose.prod.yml)

cd "$APP_DIR"

if [ ! -f .env ]; then
  echo "✗ $APP_DIR/.env is missing. Create it (see deploy/README.md), then re-run." >&2
  exit 1
fi

# Compose reads .env for ${...} interpolation; load it here too for the psql password.
set -a
# shellcheck disable=SC1091
. ./.env
set +a

previous="$(cat .last-good-tag 2>/dev/null || true)"
export IMAGE_TAG

if [ -z "${SKIP_PULL:-}" ]; then
  echo "→ pulling image ($IMAGE_TAG)"
  "${COMPOSE[@]}" pull api
fi   # SKIP_PULL is for testing against a locally built image only

# Apply the schema BEFORE the new container starts. The EF script is idempotent (guarded by
# __EFMigrationsHistory), so re-running it is safe. It runs as the application role, so the
# tables are owned by it. EF wraps each migration in its own BEGIN/COMMIT, and ON_ERROR_STOP
# aborts at the first failure, so a failing migration is rolled back and later ones never run.
if [ -f cheflan-courses.sql ]; then
  echo "→ applying schema"
  docker exec -i -e PGPASSWORD="$DB_PASSWORD" "$PG_CONTAINER" \
    psql -h 127.0.0.1 -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 -q \
    < cheflan-courses.sql
else
  echo "→ no cheflan-courses.sql next to the script; skipping schema step"
fi

echo "→ starting"
"${COMPOSE[@]}" up -d --remove-orphans

echo "→ health check (http://127.0.0.1:$API_PORT/health)"
code=""
for _ in $(seq 1 30); do
  code=$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$API_PORT/health" || true)
  if [ "$code" = "200" ]; then
    echo "   /health → 200"
    echo "$IMAGE_TAG" > .last-good-tag
    docker image prune -f >/dev/null
    exit 0
  fi
  sleep 2
done

echo "   never came back healthy (last status: ${code:-none})" >&2
"${COMPOSE[@]}" logs --tail=50 api >&2 || true
if [ -n "$previous" ] && [ "$previous" != "$IMAGE_TAG" ]; then
  echo "   rollback: IMAGE_TAG=$previous bash deploy-api.sh   (schema changes are forward-only)" >&2
fi
exit 1
