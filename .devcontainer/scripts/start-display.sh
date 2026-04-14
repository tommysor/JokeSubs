#!/usr/bin/env bash
set -euo pipefail

is_port_listening() {
    local port="$1"
    ss -ltn | awk '{print $4}' | grep -q ":${port}$"
}

# Start Xvfb virtual display on :99 if not already running.
if ! pgrep -x Xvfb > /dev/null; then
    Xvfb :99 -screen 0 1280x960x24 &
    sleep 1
fi

# Start x11vnc attached to :99 if not already running.
if ! pgrep -x x11vnc > /dev/null; then
    x11vnc -display :99 -nopw -listen localhost -xkb -forever -quiet &
    sleep 1
fi

# Start noVNC websocket proxy on port 6080 if not already running.
# Access in the browser at: http://localhost:6080/vnc.html
if ! is_port_listening 6080; then
    /usr/share/novnc/utils/novnc_proxy --vnc localhost:5900 --listen 6080 >/tmp/novnc.log 2>&1 &
fi
