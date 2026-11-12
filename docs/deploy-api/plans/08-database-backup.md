# Plan 8: nightly encrypted backup of the cheflan database to Backblaze B2

Why: there is no backup. A broken VPS disk or a wrong `DELETE` would lose every customer, order and enrollment.

## Decisions (confirmed)

| | |
|---|---|
| What | **Only the `cheflan` database** (`pg_dump -Fc`) from the shared `postgres` container. Not wish-list, not roles |
| When | Every night at 03:00 (the VPS clock is `Asia/Ho_Chi_Minh`), from the deploy user's crontab (user `nina` is in group `docker`, no sudo) |
| Where | A new **private** B2 bucket just for these backups, not the public image bucket |
| Encryption | **gpg symmetric (AES256)** before upload. The passphrase lives in the backup `.env` on the VPS and in the user's password manager. Lose it and the backups are useless |
| Keep | 14 days, enforced by a **B2 lifecycle rule** (hide after 14 days, delete 1 day later), so the VPS key needs no delete right: a compromised VPS cannot wipe the backups |
| Discord | A message **on failure and on every success** (one short line), to the same webhook as Dozzle. A missing morning line means the job did not run |
| Restore test | Not now (user's decision). The restore commands are documented |

## Build (files in `deploy/backup/`, copied to `~/chef-lan-backup/` on the VPS)

1. `backup-db.sh` (bash, `set -euo pipefail`, `umask 077`):
   - Reads `.env` next to it: `DISCORD_WEBHOOK_URL`, `BACKUP_PASSPHRASE`, `B2_KEY_ID`, `B2_APP_KEY`, `B2_BUCKET`, optional
     `PG_CONTAINER` (default `postgres`), `DB_NAME` (default `cheflan`), `RCLONE_IMAGE`.
   - A lock (`flock` on a file next to it) so two runs never overlap.
   - Dump: `docker exec <container> sh -c 'pg_dump -U "$POSTGRES_USER" -d <db> -Fc'` (the container's own superuser, its own
     pg_dump 17) into a temp dir created with `mktemp -d`, removed by a `trap` on exit.
   - Sanity check: the dump is not empty and `pg_restore --list` (run inside the same container, file piped in) succeeds.
   - Encrypt: `gpg --batch --yes --pinentry-mode loopback --symmetric --cipher-algo AES256 --passphrase-file <fd or temp file>`
     (never the passphrase on the command line: it would show in `ps`). Name: `cheflan-YYYYMMDD-HHMMSS.dump.gpg`.
   - Upload with rclone in Docker (no install on the VPS): pinned image `rclone/rclone:<version>`, B2 configured through
     `RCLONE_CONFIG_B2_*` environment variables passed with `--env-file`/`-e` (never on the command line), `rclone copyto` the
     file to `b2:<bucket>/cheflan/<name>`, then `rclone lsf` to count the backups present.
   - Discord on success: `✅ Backup cheflan OK: 2.1 MB, 14 bản trên B2`. On any failure (an `ERR` trap that knows the
     current step): `❌ Backup cheflan lỗi ở bước <step>: <last stderr line>`. Post with `curl` and
     `allowed_mentions: {parse: []}`, JSON built safely (`jq -n` if present on the VPS, otherwise careful escaping; check).
   - Exit non-zero on failure. Output to `~/chef-lan-backup/backup.log` (cron line redirects), rotated by keeping the last
     1000 lines.
   - A `BACKUP_TARGET` override (default `b2:<bucket>/cheflan`) so the script can be tested locally against an rclone
     local path, with no B2 at all.
2. `.env.example` with every variable and comments (no real values). `crontab.txt` with the one line:
   `0 3 * * * /home/nina/chef-lan-backup/backup-db.sh >> /home/nina/chef-lan-backup/backup.log 2>&1`.
3. `deploy/README.md` section "Backup": B2 bucket (private, lifecycle rule as above), application key restricted to that
   bucket with **read and write files only** (B2 "Read and Write" includes delete; note which capabilities rclone needs and
   whether a key without `deleteFiles` works for `copyto`; if it does not, say so and keep the lifecycle rule anyway),
   generate the passphrase (`openssl rand -base64 32`) and store it in the password manager first, copy the folder, fill
   `.env` (mode 600), run once by hand, install the crontab line. Restore: download, `gpg --decrypt`, `pg_restore` into a
   scratch database first, never straight over production. Replace the "Backup: none yet" row in Operations.
4. Update `docs/deploy-api/spec.md` "Open items" (backups no longer open).

## Done when

Script passes `shellcheck` (via `docker run --rm koalaman/shellcheck:stable` if not installed). Run locally against the local
Docker Postgres (`cheflan-postgres`, `PG_CONTAINER=cheflan-postgres`) with `BACKUP_TARGET` pointing at a local folder and
`DISCORD_WEBHOOK_URL` pointing at a local listener (`nc -l` or a tiny python server), never the real webhook:
the file appears, decrypts with the passphrase, `pg_restore --list` reads it; a forced failure (wrong container name) posts
the failure message and exits non-zero. Nothing touches the VPS or B2 during development.

## Implementation notes

Built (status: written and tested locally, not yet run on the VPS):

- `deploy/backup/backup-db.sh`, `.env.example`, `crontab.txt`; `deploy/README.md` section "Backup" and the Operations row; `spec.md` open item.
- The script follows the plan. Details worth knowing: the ERR trap reads a per-run stderr file (every step appends to it) and posts
  the step name and its last line (ASCII only, 500 chars); the Discord URL is given to curl through `-K -` on stdin so the webhook
  never shows in `ps`; the passphrase reaches gpg through `--passphrase-fd` from a process substitution (not on disk, not in `ps`);
  the rclone credentials go through a mode-600 `--env-file` in the temp dir; the rclone image is pinned to `rclone/rclone:1.68.2`.
- **No jq**: the JSON body is built in bash (`json_escape`: tabs to spaces, control characters dropped, `\` and `"` escaped,
  newlines to `\n`), so it works whether or not the VPS has jq.
- `BACKUP_TARGET`: an absolute local path is mounted into the rclone container at the same path (that is how the local test ran);
  anything else is passed to rclone as is. Unset = `b2:$B2_BUCKET/cheflan`. `ENV_FILE` and `LOG_FILE` can be overridden too.
  Values in `.env` win over the caller's environment (the file is sourced last), so a test that needs another value uses `ENV_FILE`.
- Lock: a second run posts a failure line (step `lock`) and exits 1; it does not disturb the running one.

Tested locally (macOS, local `cheflan-postgres`, rclone 1.68.2 in Docker against a local folder, a throwaway python listener
on 127.0.0.1 instead of Discord; everything removed afterwards): shellcheck clean; success run produced a 21 KB `.dump.gpg`,
Discord body `✅ Backup cheflan OK: 21 KB, 1 bản trên B2`; a second run counted 2 files; the file decrypts with the passphrase
(wrong one fails, rc 2) and `pg_restore --list` reads it; a wrong container name posted `❌ Backup cheflan lỗi ở bước dump: Error:
No such container: nope` and exited 1; a run while the lock was held posted the lock failure and exited 1. `flock` does not
exist on macOS, so a small python shim stood in for it in the test; the real `flock -n 9` was not exercised. macOS bash 3.2 ran the
script, Ubuntu's bash 5 should be fine. Not tested: B2 itself, the Discord webhook, cron, the upload failure path with a bad image.

Deviations: the success message says "trên B2" even for a local target (test only). `.gitignore` line 266 (`Backup*/`) matches
`deploy/backup/` on a case-insensitive filesystem (macOS): check `git status` shows the files, or add a `!deploy/backup/` exception.

B2 key capability question: **unverified** (no access to B2 or its docs during this work). Expectation: rclone `copyto` of a new,
uniquely named file needs `listBuckets`, `listFiles` and `writeFiles` (it lists the destination to see whether the file exists,
then uploads; `readFiles` is only needed for restores). It should need `deleteFiles` only when it overwrites a file or cleans
up after a failed large-file upload, and names carry a timestamp, so a key without `deleteFiles` is expected to work. Confirm on the
first real run; if it fails, the README tells the user to add the missing capability.
