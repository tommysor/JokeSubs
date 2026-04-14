#!/usr/bin/env bash
set -euo pipefail

QUIET=0
if [[ "${1:-}" == "--quiet" ]]; then
  QUIET=1
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ASPIRE_WRAPPER="$ROOT_DIR/.devcontainer/scripts/mcp-aspire.sh"
PLAYWRIGHT_WRAPPER="$ROOT_DIR/.devcontainer/scripts/mcp-playwright.sh"
START_DISPLAY="$ROOT_DIR/.devcontainer/scripts/start-display.sh"

wait_for_novnc() {
  local attempts=15

  while [[ $attempts -gt 0 ]]; do
    if curl -fsS "http://127.0.0.1:6080/vnc.html" >/dev/null 2>&1; then
      return 0
    fi
    attempts=$((attempts - 1))
    sleep 1
  done

  return 1
}

if [[ $QUIET -eq 0 ]]; then
  echo "MCP preflight: checking wrappers and commands..."
fi

[[ -x "$ASPIRE_WRAPPER" ]] || { echo "Missing executable: $ASPIRE_WRAPPER"; exit 1; }
[[ -x "$PLAYWRIGHT_WRAPPER" ]] || { echo "Missing executable: $PLAYWRIGHT_WRAPPER"; exit 1; }
[[ -x "$START_DISPLAY" ]] || { echo "Missing executable: $START_DISPLAY"; exit 1; }

bash "$START_DISPLAY"

if ! wait_for_novnc; then
  echo "noVNC is unavailable at http://127.0.0.1:6080/vnc.html" >&2
  exit 1
fi

"$ASPIRE_WRAPPER" --healthcheck
"$PLAYWRIGHT_WRAPPER" --healthcheck

if [[ $QUIET -eq 0 ]]; then
  echo "MCP preflight succeeded."
fi
