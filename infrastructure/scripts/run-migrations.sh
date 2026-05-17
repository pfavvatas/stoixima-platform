#!/usr/bin/env bash
# Run PostgreSQL migrations in order.
# Tracks applied versions in the _migrations table — safe to run multiple times.
#
# Usage:
#   ./run-migrations.sh
#   DB_HOST=myhost DB_USER=myuser DB_PASS=mypass ./run-migrations.sh

set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-stoixima}"
DB_USER="${DB_USER:-stoixima}"
export PGPASSWORD="${DB_PASS:-stoixima_dev}"

MIGRATIONS_DIR="$(cd "$(dirname "$0")/../migrations/postgres" && pwd)"

PSQL="psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME"

echo "==> Connecting to $DB_HOST:$DB_PORT/$DB_NAME as $DB_USER"

# Ensure _migrations table exists (idempotent bootstrap)
$PSQL -c "
  CREATE TABLE IF NOT EXISTS _migrations (
    version    TEXT PRIMARY KEY,
    applied_at TIMESTAMPTZ DEFAULT NOW()
  );
" >/dev/null

echo "==> Running migrations from $MIGRATIONS_DIR"

for file in "$MIGRATIONS_DIR"/V*.sql; do
    version=$(basename "$file" .sql | cut -d'_' -f1)  # e.g. V1

    already_applied=$($PSQL -t -c "SELECT COUNT(*) FROM _migrations WHERE version = '$version';" | tr -d '[:space:]')

    if [ "$already_applied" = "1" ]; then
        echo "    SKIP  $version (already applied)"
    else
        echo "    APPLY $version — $(basename "$file")"
        $PSQL -f "$file"
        echo "    OK    $version"
    fi
done

echo "==> All migrations complete."
