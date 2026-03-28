FROM mcr.microsoft.com/devcontainers/dotnet:10.0

# Configure Docker daemon to disable iptables so that Docker-in-Docker works
# when the devcontainer is hosted by Podman (which does not expose the iptables
# NAT table to nested containers).
RUN mkdir -p /etc/docker && \
    printf '{\n  "iptables": false\n}\n' > /etc/docker/daemon.json

# Install core utilities needed by MCP wrappers and browser runtime.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        git \
        jq \
        unzip \
        libatk-bridge2.0-0 \
        libatk1.0-0 \
        libcups2 \
        libdbus-1-3 \
        libdrm2 \
        libgbm1 \
        libgtk-3-0 \
        libnspr4 \
        libnss3 \
        libx11-6 \
        libx11-xcb1 \
        libxcomposite1 \
        libxdamage1 \
        libxext6 \
        libxfixes3 \
        libxkbcommon0 \
        libxrandr2 \
        xdg-utils \
    && if apt-cache show libasound2t64 >/dev/null 2>&1; then apt-get install -y --no-install-recommends libasound2t64; else apt-get install -y --no-install-recommends libasound2; fi \
    && rm -rf /var/lib/apt/lists/*
