#!/bin/bash
# =============================================================================
# Deploy Production Stack
# =============================================================================
# Usage: ./deploy-prod.sh
#
# What it does:
#   1. Pulls latest code from the "main" branch
#   2. Tears down existing containers (fixes name conflicts)
#   3. Rebuilds and starts the production stack
#   4. Cleans up unused Docker images
#
# Run from the VPS at /opt/sujanmotors/
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "=== Deploying Production Environment ==="

cd "$SCRIPT_DIR/.."
echo "--- Tearing down existing production containers ---"
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml down || true

echo "--- Building and starting production stack ---"
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml up --build -d

echo "--- Cleaning up unused Docker images ---"
docker image prune -f

echo "=== Production deployment complete! ==="
echo "Web:  http://$(hostname -I | awk '{print $1}'):4200"
