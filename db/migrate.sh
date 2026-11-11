#!/bin/bash
# Usage: bash db/migrate.sh <connection-string>
# Example: bash db/migrate.sh "postgresql://postgres:password@localhost:5432/tradeorders"

CONN="${1:-postgresql://postgres:password@localhost:5432/tradeorders}"
MIGRATIONS_DIR="$(dirname "$0")/migrations"

echo "Running migrations against: $CONN"

for file in "$MIGRATIONS_DIR"/*.sql; do
  echo "  Applying $(basename "$file")..."
  psql "$CONN" -f "$file"
  if [ $? -ne 0 ]; then
    echo "  FAILED: $(basename "$file")"
    exit 1
  fi
done

echo "All migrations applied."
