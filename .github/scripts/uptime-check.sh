#!/usr/bin/env bash
# Outside uptime check. Checks the sites, compares with the state of the previous run and
# posts to Discord only when a site changes state. Exits 0 when a site is down: down is data.
#
# Env:
#   STATE_FILE           JSON state, read at start and rewritten at end (default uptime-state.json)
#   DISCORD_WEBHOOK_URL  webhook; unset = log a notice and skip the post
#   DRY_RUN=1            print the message instead of posting
#   SITES                newline-separated "name|url|expected body text (optional)"; default = production
#   TRIES, RETRY_DELAY, TIMEOUT   3, 20 s, 15 s
#   NOW                  epoch seconds override (tests)
set -euo pipefail

STATE_FILE="${STATE_FILE:-uptime-state.json}"
TRIES="${TRIES:-3}"
RETRY_DELAY="${RETRY_DELAY:-20}"
TIMEOUT="${TIMEOUT:-15}"
NOW="${NOW:-$(date +%s)}"
DEFAULT_SITES='courses.bonbocon.com|https://courses.bonbocon.com/|
admin.bonbocon.com|https://admin.bonbocon.com/|<div id="root">
api.bonbocon.com|https://api.bonbocon.com/api/v1/health|'
SITES="${SITES:-$DEFAULT_SITES}"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

check_site() {
  local name="$1" url="$2" expect="$3" out="$4" code reason="" i
  for ((i = 1; i <= TRIES; i++)); do
    code="$(curl -sS -o "$work/$name.body" -w '%{http_code}' --max-time "$TIMEOUT" "$url" 2>/dev/null || true)"
    code="${code:-000}"
    if [[ "$code" == "200" ]]; then
      if [[ -z "$expect" ]] || grep -qF -- "$expect" "$work/$name.body"; then
        printf 'up\t\n' >"$out"
        return
      fi
      reason="nội dung không đúng"
    elif [[ "$code" == "000" ]]; then
      reason="không có phản hồi"
    else
      reason="HTTP $code"
    fi
    ((i < TRIES)) && sleep "$RETRY_DELAY"
  done
  printf 'down\t%s\n' "$reason" >"$out"
}

duration() {
  local mins=$(($1 / 60))
  if ((mins < 1)); then echo "dưới 1 phút"
  elif ((mins < 60)); then echo "${mins} phút"
  else echo "$((mins / 60)) giờ $((mins % 60)) phút"; fi
}

names=()
while IFS='|' read -r name url expect; do
  [[ -z "$name" ]] && continue
  names+=("$name")
  check_site "$name" "$url" "$expect" "$work/$name.result" &
done <<<"$SITES"
wait

prev='{}'
if [[ -s "$STATE_FILE" ]] && jq -e type "$STATE_FILE" >/dev/null 2>&1; then
  prev="$(cat "$STATE_FILE")"
fi

new="$prev"
lines=()
for name in "${names[@]}"; do
  IFS=$'\t' read -r now_state reason <"$work/$name.result"
  was="$(jq -r --arg n "$name" '.[$n].state // "up"' <<<"$prev")"
  since="$(jq -r --arg n "$name" '.[$n].since // empty' <<<"$prev")"
  if [[ "$now_state" == "down" && "$was" == "up" ]]; then
    lines+=("🔴 $name không truy cập được (${reason})")
    since="$NOW"
  elif [[ "$now_state" == "up" && "$was" == "down" ]]; then
    lines+=("🟢 $name đã hoạt động lại sau $(duration $((NOW - ${since:-$NOW})))")
    since="$NOW"
  fi
  since="${since:-$NOW}"
  new="$(jq --arg n "$name" --arg s "$now_state" --argjson t "$since" '.[$n] = {state: $s, since: $t}' <<<"$new")"
  echo "$name: $now_state${reason:+ ($reason)}"
done

if ((${#lines[@]} > 0)); then
  message="$(printf '%s\n' "${lines[@]}")"
  if [[ "${DRY_RUN:-}" == "1" ]]; then
    echo "DRY_RUN message:"
    echo "$message"
  elif [[ -z "${DISCORD_WEBHOOK_URL:-}" ]]; then
    echo "::notice::DISCORD_WEBHOOK_URL is not set; not posting: ${message//$'\n'/ / }"
  else
    payload="$(jq -n --arg c "$message" '{content: $c, allowed_mentions: {parse: []}}')"
    # Keep the old state when the post fails, so the next run alerts again.
    curl -sS --fail --max-time 20 -H 'Content-Type: application/json' -d "$payload" "$DISCORD_WEBHOOK_URL" >/dev/null \
      || { echo "::error::Discord post failed"; exit 1; }
  fi
fi

printf '%s\n' "$new" >"$STATE_FILE"
