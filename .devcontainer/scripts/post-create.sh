#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

export PATH="$HOME/.dotnet/tools:$PATH"

if ! command -v aspire >/dev/null 2>&1; then
  dotnet tool install --global Aspire.Cli
fi

if command -v npm >/dev/null 2>&1; then
  npm install -g @playwright/mcp
fi

bash "$ROOT_DIR/.devcontainer/scripts/check-mcp.sh"
