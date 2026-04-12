#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APPHOST_PATH="$ROOT_DIR/JokeSubs.AppHost/JokeSubs.AppHost.csproj"

export PATH="$HOME/.dotnet/tools:$PATH"

wait_for_docker_daemon() {
  local attempts=45

  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker CLI is unavailable; skipping daemon readiness check."
    return
  fi

  echo "Waiting for Docker daemon to become ready..."
  until docker info >/dev/null 2>&1; do
    attempts=$((attempts - 1))
    if [[ $attempts -le 0 ]]; then
      echo "Docker daemon did not become ready in time." >&2
      return 1
    fi
    sleep 2
  done
}

warmup_aspire_resources() {
  if [[ ! -f "$APPHOST_PATH" ]]; then
    echo "AppHost project not found at $APPHOST_PATH; skipping Aspire warm-up."
    return
  fi

  if ! command -v aspire >/dev/null 2>&1; then
    echo "Aspire CLI is unavailable; skipping Aspire warm-up."
    return
  fi

  echo "Warming up Aspire resources for first test run..."
  aspire start --isolated --apphost "$APPHOST_PATH" --non-interactive --nologo
  aspire wait server --apphost "$APPHOST_PATH" --timeout 240 --non-interactive --nologo
  aspire wait webfrontend --apphost "$APPHOST_PATH" --timeout 240 --non-interactive --nologo
  aspire stop --apphost "$APPHOST_PATH" --non-interactive --nologo
}

install_nodejs_playwright_browsers() {
  if ! command -v npm >/dev/null 2>&1; then
    echo "npm is unavailable; skipping Node.js Playwright browser install."
    return
  fi

  echo "Installing Playwright browser binaries for Node.js MCP server..."
  npx --yes playwright install chromium chrome
}

ensure_xvfb_running() {
  if ! command -v Xvfb >/dev/null 2>&1; then
    echo "Xvfb is unavailable; skipping virtual display setup."
    return
  fi

  # Check if DISPLAY is already set to a valid Xvfb display
  if [[ -n "${DISPLAY}" ]] && [[ "${DISPLAY}" == :* ]]; then
    if ps aux | grep -q "[X]vfb ${DISPLAY#:}"; then
      echo "Xvfb already running on ${DISPLAY}"
      return
    fi
  fi

  # Start Xvfb on display :99 if not already running
  local display=":99"
  if ! ps aux | grep -q "[X]vfb ${display#:}"; then
    echo "Starting Xvfb on ${display}..."
    Xvfb "$display" -screen 0 1920x1080x24 >/dev/null 2>&1 &
    export DISPLAY="$display"
    sleep 2
    echo "Xvfb started on ${DISPLAY}"
  fi
}

install_dotnet_playwright_browsers() {
  local tests_project="$ROOT_DIR/JokeSubs.AcceptanceTests/JokeSubs.AcceptanceTests.csproj"
  local playwright_ps1

  if [[ ! -f "$tests_project" ]]; then
    echo "Acceptance test project not found at $tests_project; skipping Playwright browser install."
    return
  fi

  echo "Building acceptance tests to bootstrap Playwright CLI..."
  dotnet build "$tests_project" --nologo --verbosity minimal

  playwright_ps1="$(find "$ROOT_DIR/JokeSubs.AcceptanceTests/bin" -type f -path "*/playwright.ps1" | sort | tail -n 1)"
  echo "Located Playwright PowerShell CLI at: $playwright_ps1"

  if command -v pwsh >/dev/null 2>&1 && [[ -n "$playwright_ps1" ]]; then
    echo "Installing Playwright Chromium browser for .NET tests via pwsh..."
    pwsh "$playwright_ps1" install chromium
    return
  fi

  echo "Failed to install Playwright Chromium browser for .NET tests." >&2
  return 1
}

if ! command -v aspire >/dev/null 2>&1; then
  dotnet tool install --global Aspire.Cli
fi

if command -v npm >/dev/null 2>&1; then
  npm install -g @playwright/mcp
fi

wait_for_docker_daemon
ensure_xvfb_running
install_nodejs_playwright_browsers
install_dotnet_playwright_browsers
warmup_aspire_resources

bash "$ROOT_DIR/.devcontainer/scripts/check-mcp.sh"
