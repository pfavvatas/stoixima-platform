#!/usr/bin/env bash
# One-command startup for the Stoixima platform.
# Usage: ./start.sh [--build] [--infra-only]
#
# --build       Force rebuild of all Docker images
# --infra-only  Start only infrastructure (Kafka, Postgres, ClickHouse, Redis)
set -euo pipefail

cd "$(dirname "$0")"

BUILD_FLAG=""
PROFILE=""

for arg in "$@"; do
  case $arg in
    --build)      BUILD_FLAG="--build" ;;
    --infra-only) PROFILE="--profile infra" ;;
  esac
done

# Copy .env.example → .env if not present
if [ ! -f .env ]; then
  echo "No .env found — copying .env.example. Edit it before adding real API keys."
  cp .env.example .env
fi

echo "Starting Stoixima platform..."
echo "  Feed UI:   http://localhost:3000"
echo "  Admin UI:  http://localhost:3000/admin"
echo "  API:       http://localhost:5000"
echo "  Swagger:   http://localhost:5000/swagger"
echo "  Kafka UI:  http://localhost:8080"
echo "  Grafana:   http://localhost:3001  (admin / admin)"
echo ""

docker compose up -d $BUILD_FLAG $PROFILE

echo ""
echo "All services started. Run 'docker compose logs -f' to follow logs."
