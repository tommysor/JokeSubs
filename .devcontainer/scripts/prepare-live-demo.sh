#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APPHOST_PATH="$ROOT_DIR/JokeSubs.AppHost/JokeSubs.AppHost.csproj"

export PATH="$HOME/.dotnet/tools:$PATH"

if [[ ! -f "$APPHOST_PATH" ]]; then
  echo "AppHost project not found at $APPHOST_PATH" >&2
  exit 1
fi

if ! command -v aspire >/dev/null 2>&1; then
  echo "Aspire CLI is unavailable. Run: dotnet tool install --global Aspire.Cli" >&2
  exit 1
fi

bash "$ROOT_DIR/.devcontainer/scripts/check-mcp.sh"

echo "Starting AppHost for live demo..."
aspire start --isolated --apphost "$APPHOST_PATH" --non-interactive --nologo
aspire wait server --apphost "$APPHOST_PATH" --timeout 240 --non-interactive --nologo
aspire wait webfrontend --apphost "$APPHOST_PATH" --timeout 240 --non-interactive --nologo

describe_json="$(aspire describe --apphost "$APPHOST_PATH" --format json)"
dashboard_url="$(echo "$describe_json" | jq -r '[.resources[].dashboardUrl | select(. != null)][0] // ""')"
frontend_url="$(echo "$describe_json" | jq -r '[.resources[] | select(.displayName=="webfrontend") | .urls[]?.url][0] // ""')"

echo

echo "Live demo is ready."
echo "Open these in the VS Code integrated browser:"
echo "  - noVNC viewer: http://localhost:6080/vnc.html"
if [[ -n "$dashboard_url" ]]; then
  echo "  - Aspire dashboard: $dashboard_url"
else
  echo "  - Aspire dashboard: (run 'aspire describe --apphost \"$APPHOST_PATH\" --format json' to find it)"
fi

if [[ -n "$frontend_url" ]]; then
  echo "  - Web frontend: $frontend_url"
else
  echo "  - Web frontend: (open the webfrontend endpoint from the Aspire dashboard)"
fi

echo "Use the web frontend URL in noVNC and watch Playwright actions live."
