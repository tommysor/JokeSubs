#!/usr/bin/env bash
set -euo pipefail

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker CLI is unavailable." >&2
  exit 1
fi

if docker info >/dev/null 2>&1; then
  exit 0
fi

echo "Waiting for Docker daemon to become ready..."
start_attempted=false
for ((attempt = 0; attempt < 45; attempt++)); do
  if docker info >/dev/null 2>&1; then
    exit 0
  fi

  # The Docker-in-Docker feature can stop a slow-starting daemon during its retries.
  if [[ "$start_attempted" == false && -z "${DOCKER_HOST:-}" && -z "${DOCKER_CONTEXT:-}" ]] \
    && [[ "$(docker context show)" == default ]] && ! pgrep -x dockerd >/dev/null; then
    if ! command -v dockerd >/dev/null 2>&1 || ! sudo -n true; then
      echo "Docker is unavailable and the nested daemon cannot be started." >&2
      exit 1
    fi

    echo "Starting the Docker-in-Docker daemon..."
    sudo -n sh -c 'exec dockerd >>/tmp/dockerd.log 2>&1' </dev/null &
    start_attempted=true
  fi
  sleep 2
done

echo "Docker daemon did not become ready in time. Docker diagnostics:" >&2
docker info >&2 || true
if [[ -r /tmp/dockerd.log ]]; then
  echo "Last 30 lines of /tmp/dockerd.log:" >&2
  tail -n 30 /tmp/dockerd.log >&2
fi
exit 1
