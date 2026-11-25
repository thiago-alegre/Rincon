#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/rincon}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-rincon}"
DB_USER="${DB_USER:-rincon_app}"
RCLONE_REMOTE="${RCLONE_REMOTE:-}"
DRIVE_BACKUP_DIR="${DRIVE_BACKUP_DIR:-Backups Rincon}"
RCLONE_CONFIG_FILE="${RCLONE_CONFIG_FILE:-/root/.config/rclone/rclone.conf}"
DRIVE_RETENTION_DAYS="${DRIVE_RETENTION_DAYS:-30}"

if [[ -z "${PGPASSWORD:-}" ]]; then
  echo "ERROR: PGPASSWORD is required." >&2
  exit 1
fi

timestamp="$(date +%Y%m%d_%H%M%S)"
backup_file="${BACKUP_DIR}/${DB_NAME}_${timestamp}.dump"

mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"

pg_dump \
  --host "$DB_HOST" \
  --port "$DB_PORT" \
  --username "$DB_USER" \
  --format custom \
  --blobs \
  --no-owner \
  --no-privileges \
  --file "$backup_file" \
  "$DB_NAME"

chmod 600 "$backup_file"
find "$BACKUP_DIR" -type f -name "${DB_NAME}_*.dump" -mtime +"$RETENTION_DAYS" -delete

echo "Backup created: $backup_file"

# La copia en Drive es adicional. Si falla, el backup local anterior se conserva
# y el proceso informa la advertencia en el log sin modificar el flujo local.
if [[ -n "$RCLONE_REMOTE" ]]; then
  remote_directory="${RCLONE_REMOTE}:${DRIVE_BACKUP_DIR}"
  remote_file="${remote_directory}/$(basename "$backup_file")"

  if ! command -v rclone >/dev/null 2>&1; then
    echo "WARNING: Local backup created, but rclone is not installed; Drive upload skipped." >&2
  elif [[ ! -f "$RCLONE_CONFIG_FILE" ]]; then
    echo "WARNING: Local backup created, but rclone config was not found at $RCLONE_CONFIG_FILE; Drive upload skipped." >&2
  elif rclone --config "$RCLONE_CONFIG_FILE" copyto "$backup_file" "$remote_file"; then
    echo "Drive backup uploaded: $remote_file"

    if ! rclone --config "$RCLONE_CONFIG_FILE" delete "$remote_directory" \
      --min-age "${DRIVE_RETENTION_DAYS}d" \
      --include "${DB_NAME}_*.dump"; then
      echo "WARNING: Drive upload succeeded, but old remote backups could not be removed." >&2
    fi
  else
    echo "WARNING: Local backup created, but Drive upload failed." >&2
  fi
fi
