#!/usr/bin/env bash
# Spin up the whole HoneKit stack in its deployed shape.
# If .env.local exists, its AI__* values (including the key) are loaded for the deploy
# and inherited by Docker Compose; nothing is written into a committed file.
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ -f .env.local ]]; then
  set -a
  # shellcheck disable=SC1091
  source ./.env.local
  set +a
  echo "Loaded AI settings from .env.local"
else
  echo "No .env.local: using the fixture reader"
fi

aspire deploy -o aspire-output
