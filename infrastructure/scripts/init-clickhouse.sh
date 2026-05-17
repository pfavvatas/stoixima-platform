#!/usr/bin/env bash
# Initialize ClickHouse schema — runs migrations in order.
# Tracks applied versions in stoixima._migrations (ReplacingMergeTree).
# Safe to run multiple times.
#
# Usage:
#   ./init-clickhouse.sh
#   CH_HOST=myhost CH_USER=myuser CH_PASS=mypass ./init-clickhouse.sh

set -euo pipefail

CH_HOST="${CH_HOST:-localhost}"
CH_PORT="${CH_PORT:-8123}"            # HTTP interface
CH_USER="${CH_USER:-stoixima}"
CH_PASS="${CH_PASS:-stoixima_dev}"

MIGRATIONS_DIR="$(cd "$(dirname "$0")/../migrations/clickhouse" && pwd)"

# Run SQL via HTTP interface (works without clickhouse-client installed locally)
ch_query() {
    curl -s -f \
        "http://$CH_HOST:$CH_PORT/" \
        --user "$CH_USER:$CH_PASS" \
        --data-binary "$1"
}

echo "==> Connecting to ClickHouse at $CH_HOST:$CH_PORT"

# Wait until ClickHouse is ready (up to 30s)
for i in $(seq 1 30); do
    if ch_query "SELECT 1" >/dev/null 2>&1; then
        echo "    ClickHouse is up."
        break
    fi
    echo "    Waiting for ClickHouse... ($i/30)"
    sleep 1
done

# Bootstrap: run V1 unconditionally to ensure DB + _migrations exist
echo "==> Bootstrapping database and migrations table..."
ch_query "$(cat "$MIGRATIONS_DIR/V1__init.sql")" >/dev/null

echo "==> Running migrations from $MIGRATIONS_DIR"

for file in "$MIGRATIONS_DIR"/V*.sql; do
    version=$(basename "$file" .sql | cut -d'_' -f1)  # e.g. V1

    # Skip V1 — already applied in bootstrap above
    if [ "$version" = "V1" ]; then
        echo "    SKIP  $version (bootstrap)"
        continue
    fi

    already_applied=$(ch_query \
        "SELECT count() FROM stoixima._migrations FINAL WHERE version = '$version'" \
        | tr -d '[:space:]')

    if [ "$already_applied" = "1" ]; then
        echo "    SKIP  $version (already applied)"
    else
        echo "    APPLY $version — $(basename "$file")"
        ch_query "$(cat "$file")" >/dev/null
        echo "    OK    $version"
    fi
done

echo ""
echo "==> Schema verification:"
ch_query "
SELECT
    table,
    formatReadableSize(sum(bytes_on_disk)) AS size_on_disk,
    sum(rows)                              AS total_rows
FROM system.parts
WHERE database = 'stoixima' AND active
GROUP BY table
ORDER BY table
FORMAT PrettyCompact
"

echo ""
echo "==> Applied migrations:"
ch_query "SELECT version, applied_at FROM stoixima._migrations FINAL ORDER BY version FORMAT PrettyCompact"

echo ""
echo "==> ClickHouse init complete."
