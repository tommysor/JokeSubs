#!/usr/bin/env bash
set -euo pipefail

QUIET=0
if [[ "${1:-}" == "--quiet" ]]; then
  QUIET=1
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ASPIRE_WRAPPER="$ROOT_DIR/.devcontainer/scripts/mcp-aspire.sh"
PLAYWRIGHT_WRAPPER="$ROOT_DIR/.devcontainer/scripts/mcp-playwright.sh"

if [[ $QUIET -eq 0 ]]; then
  echo "MCP preflight: checking wrappers and commands..."
fi

[[ -x "$ASPIRE_WRAPPER" ]] || { echo "Missing executable: $ASPIRE_WRAPPER"; exit 1; }
[[ -x "$PLAYWRIGHT_WRAPPER" ]] || { echo "Missing executable: $PLAYWRIGHT_WRAPPER"; exit 1; }

"$ASPIRE_WRAPPER" --healthcheck
"$PLAYWRIGHT_WRAPPER" --healthcheck

if [[ $QUIET -eq 0 ]]; then
  echo "MCP preflight succeeded."
fi
