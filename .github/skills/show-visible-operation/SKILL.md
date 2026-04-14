---
name: show-visible-operation
description: 'Show a user a live, visible browser automation in dev containers using noVNC and Playwright. Use when user says: show me live, visible browser, watch you do it, do operation in front of me, add location while I watch, demo in VS Code integrated browser.'
argument-hint: 'Operation to demonstrate in UI, plus optional target value (for example: add location location-1717)'
user-invocable: true
---

# Show Visible Operation

Demonstrate an operation live in a browser the user can watch.

## When to Use
- User asks to watch actions live instead of screenshots.
- Environment is a dev container where headed browsers are not directly visible on host.
- User wants to observe behavior directly, including unexpected or incorrect behavior.

## Inputs
- Operation intent in plain language.
- Optional target value to create or modify (for example a location id).

## Procedure
1. Run one-command live demo prep.
- Run `bash .devcontainer/scripts/prepare-live-demo.sh`.
- This starts display services, validates MCP wrappers, starts Aspire with `aspire start --isolated`, and waits for `server` and `webfrontend`.

2. Open a visible browser surface in VS Code.
- Open `http://localhost:6080/vnc.html` in the VS Code integrated browser.
- If available, use the `open_browser_page` tool for this URL.
- Tell the user this noVNC page is the live screen they should watch.

3. Open the app endpoint to be demonstrated.
- Use the dashboard URL and frontend URL printed by `prepare-live-demo.sh`.
- If needed, rediscover endpoints with `aspire describe --apphost JokeSubs.AppHost/JokeSubs.AppHost.csproj --format json`.
- In the noVNC browser, navigate to the frontend endpoint.

4. Perform the operation visibly.
- Use Playwright MCP first.
- Keep action pace human-visible (short delays between key steps).
- Prefer visible selectors and labels; avoid brittle hidden-attribute coupling.

5. Fallback path if MCP cannot drive the demo reliably.
- Run terminal Playwright headed on `DISPLAY=:99`.
- Keep motion slow enough for user observation.
- Explicitly disclose fallback usage and reason.

6. Report result clearly.
- State what was performed.
- State where the user watched it (VS Code integrated noVNC page).
- State what visible behavior occurred, including mismatches.

## Decision Points
- If prep script fails: resolve the reported failing prerequisite first, then rerun prep.
- If noVNC endpoint is down: rerun `bash .devcontainer/scripts/start-display.sh` and `bash .devcontainer/scripts/check-mcp.sh`.
- If MCP Playwright fails with missing browser: run `npx --yes playwright install chromium chrome` and retry.
- If MCP Playwright fails with X/display/session issues: use terminal fallback on `DISPLAY=:99`.
- If frontend endpoint is unknown: open Aspire dashboard and use the `webfrontend` endpoint there.
- If the observed behavior differs from expected: keep the run visible and describe the mismatch.

## Quality Checks
- `bash .devcontainer/scripts/prepare-live-demo.sh` succeeds before automation.
- noVNC page is reachable before starting automation.
- The user is told exactly where to watch.
- Operation is visible at human speed, not instant headless execution.
- Any limitations or fallbacks used are explicitly disclosed.

## Completion Criteria
- User could watch a visible browser session perform the requested operation.
- Result summary describes what was shown on screen, including any incorrect behavior that appeared.
