#!/usr/bin/env bash
# Nightly encrypted backup of the cheflan database to Backblaze B2. Design: docs/deploy-api/plans/08-database-backup.md
# Runs from the deploy user's crontab; reads .env next to this file. Posts one Discord line on success and on failure.
set -Eeuo pipefail
umask 077

DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ENV_FILE=${ENV_FILE:-$DIR/.env}
LOG_FILE=${LOG_FILE:-$DIR/backup.log}

if [[ ! -f $ENV_FILE ]]; then
  echo "$(date '+%F %T') backup: missing $ENV_FILE" >&2
  exit 1
fi
set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

PG_CONTAINER=${PG_CONTAINER:-postgres}
DB_NAME=${DB_NAME:-cheflan}
RCLONE_IMAGE=${RCLONE_IMAGE:-rclone/rclone:1.68.2}
: "${DISCORD_WEBHOOK_URL:?}" "${BACKUP_PASSPHRASE:?}"

STEP=start
WORK=
ERR=

# JSON string body without jq (the VPS may not have it): control characters out, then backslash and quote escaped.
json_escape() {
  printf '%s' "$1" | tr '\t' ' ' | tr -d '\r\000-\010\013\014\016-\037' |
    sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' | awk 'NR > 1 { printf "\\n" } { printf "%s", $0 }'
}

# The webhook URL is a credential, so it goes through curl's stdin config rather than the command line (ps).
notify() {
  local payload
  payload=$(printf '{"content":"%s","allowed_mentions":{"parse":[]}}' "$(json_escape "$1")")
  if ! printf 'url = "%s"\n' "$DISCORD_WEBHOOK_URL" |
    curl -sS --fail --max-time 20 -K - -H 'Content-Type: application/json; charset=utf-8' -d "$payload" -o /dev/null; then
    echo "$(date '+%F %T') backup: could not post to Discord" >&2
  fi
}

on_error() {
  local rc=$?
  trap - ERR
  local last=
  if [[ -n $ERR && -s $ERR ]]; then
    cat "$ERR" >&2
    last=$(LC_ALL=C tr -cd '\11\12\40-\176' <"$ERR" | awk 'NF { l = $0 } END { print substr(l, 1, 500) }')
  fi
  echo "$(date '+%F %T') backup FAILED at step '$STEP' (exit $rc)" >&2
  notify "❌ Backup $DB_NAME lỗi ở bước $STEP: ${last:-exit $rc}"
  exit "$rc"
}

cleanup() {
  [[ -n $WORK && -d $WORK ]] && rm -rf "$WORK"
  return 0
}

trap on_error ERR
trap cleanup EXIT

command -v flock >/dev/null || { echo "backup: flock is required" >&2; exit 1; }
exec 9>"$DIR/.backup.lock"
if ! flock -n 9; then
  STEP=lock
  ERR=
  echo "$(date '+%F %T') backup: another run holds the lock" >&2
  notify "❌ Backup $DB_NAME lỗi ở bước lock: lần chạy trước vẫn đang chạy"
  exit 1
fi

if [[ -f $LOG_FILE ]]; then
  tail -n 1000 "$LOG_FILE" >"$LOG_FILE.tmp" && cat "$LOG_FILE.tmp" >"$LOG_FILE"
  rm -f "$LOG_FILE.tmp"
fi
echo "$(date '+%F %T') backup: start"

WORK=$(mktemp -d)
ERR=$WORK/stderr.txt
: >"$ERR"
NAME=cheflan-$(date +%Y%m%d-%H%M%S).dump.gpg
DUMP=$WORK/dump.pgc
ENC=$WORK/$NAME

STEP=dump
# The container's own superuser and its own pg_dump; the custom format is compressed already.
docker exec "$PG_CONTAINER" sh -c "pg_dump -U \"\$POSTGRES_USER\" -d '$DB_NAME' -Fc" >"$DUMP" 2>>"$ERR"

STEP=check
if [[ ! -s $DUMP ]]; then
  echo "the dump is empty" >>"$ERR"
  false
fi
docker exec -i "$PG_CONTAINER" pg_restore --list <"$DUMP" >/dev/null 2>>"$ERR"

STEP=encrypt
# Passphrase through a file descriptor: never on the command line (ps) and never on disk.
gpg --batch --yes --quiet --pinentry-mode loopback --symmetric --cipher-algo AES256 \
  --passphrase-fd 3 -o "$ENC" "$DUMP" 3< <(printf '%s' "$BACKUP_PASSPHRASE") 2>>"$ERR"
rm -f "$DUMP"
SIZE=$(wc -c <"$ENC" | tr -d ' ')

STEP=upload
MOUNT=()
if [[ -n ${BACKUP_TARGET:-} ]]; then
  # A local absolute path (tests): rclone runs in a container, so mount the folder at the same path.
  TARGET=$BACKUP_TARGET
  if [[ $TARGET == /* ]]; then
    mkdir -p "$TARGET"
    MOUNT=(-v "$TARGET:$TARGET")
  fi
else
  : "${B2_KEY_ID:?}" "${B2_APP_KEY:?}" "${B2_BUCKET:?}"
  TARGET=b2:$B2_BUCKET/cheflan
  printf 'RCLONE_CONFIG_B2_TYPE=b2\nRCLONE_CONFIG_B2_ACCOUNT=%s\nRCLONE_CONFIG_B2_KEY=%s\n' "$B2_KEY_ID" "$B2_APP_KEY" >"$WORK/rclone.env"
fi
[[ -f $WORK/rclone.env ]] || : >"$WORK/rclone.env"

rclone() {
  docker run --rm --user "$(id -u):$(id -g)" --env-file "$WORK/rclone.env" -e HOME=/tmp \
    -v "$WORK:/in:ro" ${MOUNT[@]+"${MOUNT[@]}"} "$RCLONE_IMAGE" "$@"
}

rclone copyto "/in/$NAME" "$TARGET/$NAME" 2>>"$ERR"

STEP=list
COUNT=$(rclone lsf --files-only "$TARGET" 2>>"$ERR" | grep -c '\.dump\.gpg$' || true)

if ((SIZE >= 1048576)); then
  HUMAN=$(LC_ALL=C awk -v b="$SIZE" 'BEGIN { printf "%.1f MB", b / 1048576 }')
else
  HUMAN=$(LC_ALL=C awk -v b="$SIZE" 'BEGIN { printf "%.0f KB", b / 1024 }')
fi

echo "$(date '+%F %T') backup: $NAME $HUMAN, $COUNT on target"
notify "✅ Backup $DB_NAME OK: $HUMAN, $COUNT bản trên B2"
