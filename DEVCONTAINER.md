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
aspire run
```

2. Use MCP tools after preflight passes:
   - Aspire MCP wrapper: `.devcontainer/scripts/mcp-aspire.sh`
   - Playwright MCP wrapper: `.devcontainer/scripts/mcp-playwright.sh`

3. If `JokeSubs.AppHost/AppHost.cs` changes, restart Aspire.

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

### Aspire resources seem stale

Stop and restart `aspire run`. App model changes in `JokeSubs.AppHost/AppHost.cs` require restart.
