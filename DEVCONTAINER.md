# Dev Container Workflow

This repository supports running Aspire resources and MCP tooling from inside a VS Code dev container.

## First-time setup

1. Open the repository in VS Code.
2. Run "Dev Containers: Reopen in Container".
3. Wait for post-create to finish. It installs Aspire CLI tooling, waits for Docker readiness, installs Playwright browser binaries for both Node.js MCP server and .NET UI tests, warms up Aspire resources once, and validates MCP wrappers.
4. Run this check manually if needed:

```bash
bash .devcontainer/scripts/check-mcp.sh
```

## Daily workflow

1. Start the app model:

```bash
aspire start --isolated
```

2. Use MCP tools after preflight passes:
   - Aspire MCP wrapper: `.devcontainer/scripts/mcp-aspire.sh`
   - Playwright MCP wrapper: `.devcontainer/scripts/mcp-playwright.sh`

3. If `JokeSubs.AppHost/AppHost.cs` changes, run `aspire start --isolated` again.

## Live demo workflow (VS Code integrated browser)

Use this flow when you need to visibly demonstrate actions to the user.

1. Run one prep command:

```bash
bash .devcontainer/scripts/prepare-live-demo.sh
```

2. Open the VS Code integrated browser to:
   - `http://localhost:6080/vnc.html` (live noVNC screen)
   - Aspire dashboard URL printed by `prepare-live-demo.sh` (dynamic under `--isolated`)
   - Web frontend URL printed by `prepare-live-demo.sh`

3. If you need to rediscover endpoints, run:

```bash
aspire describe --apphost JokeSubs.AppHost/JokeSubs.AppHost.csproj --format json
```

4. Perform the operation through Playwright MCP. The user watches it live in noVNC.

### What the prep command does

- Starts/reuses Xvfb, x11vnc, and noVNC display services.
- Verifies noVNC is reachable on port `6080`.
- Runs MCP wrapper health checks.
- Starts AppHost with `aspire start --isolated`.
- Waits for `server` and `webfrontend` to become ready.
- Prints the active Aspire dashboard URL and webfrontend URL for the current run.

## MCP reliability guardrails

- MCP commands are pinned to wrapper scripts from `.vscode/mcp.json`.
- Wrappers verify command availability before startup.
- A shared preflight script detects command/path drift after container create/reopen.
- Playwright MCP wrapper falls back to `npx -y @playwright/mcp` if the global binary is unavailable.

## Troubleshooting

### MCP preflight fails

Run:

```bash
bash .devcontainer/scripts/check-mcp.sh
```

Common fixes:

1. Re-run post-create bootstrap:

```bash
bash .devcontainer/scripts/post-create.sh
```

2. Ensure dotnet tools path is available:

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
```

3. Verify core commands:

```bash
aspire --version
npx -y @playwright/mcp --help
```

### noVNC page does not open

Run:

```bash
bash .devcontainer/scripts/start-display.sh
bash .devcontainer/scripts/check-mcp.sh
```

Then reload `http://localhost:6080/vnc.html` in the VS Code integrated browser.

### Aspire resources seem stale

Run `aspire start --isolated` again. App model changes in `JokeSubs.AppHost/AppHost.cs` require restart.
