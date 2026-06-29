#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/rincon}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-rincon}"
DB_USER="${DB_USER:-rincon_app}"

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
