#!/usr/bin/env bash
set -euo pipefail

run_playwright_mcp() {
  local default_args=("--headless" "--no-sandbox")

  if command -v playwright-mcp >/dev/null 2>&1; then
    exec playwright-mcp "${default_args[@]}" "$@"
  fi

  if command -v npx >/dev/null 2>&1; then
    exec npx -y @playwright/mcp "${default_args[@]}" "$@"
  fi

  echo "Neither playwright-mcp nor npx is available" >&2
  exit 1
}

if [[ "${1:-}" == "--healthcheck" ]]; then
  if command -v playwright-mcp >/dev/null 2>&1; then
    playwright-mcp --help >/dev/null
    exit 0
  fi

  if command -v npx >/dev/null 2>&1; then
    npx -y @playwright/mcp --help >/dev/null
    exit 0
  fi

  echo "playwright MCP unavailable (missing playwright-mcp and npx)" >&2
  exit 1
fi

run_playwright_mcp "$@"
