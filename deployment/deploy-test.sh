#!/bin/bash
# =============================================================================
# Deploy Test/Staging Stack
# =============================================================================
# Usage: ./deploy-test.sh
#
# What it does:
#   1. Tears down existing containers (fixes name conflicts)
#   2. Rebuilds and starts the test stack
#   3. Cleans up unused Docker images
#
# Pull the latest "test" branch first (git pull origin test).
# Run from the VPS at /opt/sujanmotors-test/
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "=== Deploying Test Environment ==="

cd "$SCRIPT_DIR"
echo "--- Tearing down existing test containers ---"
docker compose -p sujanmotors-test --env-file .env -f docker-compose.test.yml down || true

echo "--- Building and starting test stack ---"
docker compose -p sujanmotors-test --env-file .env -f docker-compose.test.yml up --build -d

echo "--- Cleaning up unused Docker images ---"
docker image prune -f

echo "=== Test deployment complete! ==="
echo "Web:  http://$(hostname -I | awk '{print $1}'):4201"
