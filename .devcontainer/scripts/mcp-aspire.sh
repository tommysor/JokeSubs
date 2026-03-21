#!/usr/bin/env bash
set -euo pipefail

export PATH="$HOME/.dotnet/tools:$PATH"

if [[ "${1:-}" == "--healthcheck" ]]; then
  command -v aspire >/dev/null 2>&1 || {
    echo "aspire command not found" >&2
    exit 1
  }
  aspire --version >/dev/null
  exit 0
fi

exec aspire mcp start "$@"
