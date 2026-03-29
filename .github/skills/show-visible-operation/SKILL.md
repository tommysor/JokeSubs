---
name: show-visible-operation
description: 'Show a user a live, visible browser automation in dev containers using Xvfb/noVNC and Playwright MCP or terminal Playwright fallback. Use when user says: show me live, visible browser, watch you do it, do operation in front of me, add location while I watch.'
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
1. Confirm app readiness.
- If apphost is not running, start it with aspire run.
- Capture the current frontend endpoint from Aspire output or logs.

2. Confirm display stack readiness.
- Ensure Xvfb is running on display :99.
- Ensure x11vnc and noVNC proxy are running.
- Ensure port 6080 is listening.

3. Open user-visible noVNC view.
- Open http://localhost:6080/vnc.html in the host browser.
- Tell the user this is the window where actions will be visible.

4. Attempt automation using Playwright MCP first.
- Navigate to frontend endpoint.
- Perform requested operation.
- If MCP cannot run headed browser due to display/session mismatch, switch to fallback path.

5. Fallback path: terminal Playwright on DISPLAY=:99.
- Ensure Playwright browser binaries are installed for the runtime being used.
- Run a headed Playwright script with slow motion so the user can watch the flow.
- Keep waits short but visible for confirmation.
- If the fallback path is used, explicitly disclose this to the user and explain why.

6. Report result clearly.
- State what was performed.
- State where the user could see it.
- State the visible behavior that was observed.

## Decision Points
- If noVNC endpoint is down: start or restart display services, then retry.
- If MCP Playwright fails with missing browser: install browser and retry.
- If MCP Playwright fails with missing X server: use terminal Playwright fallback on DISPLAY=:99.
- If frontend endpoint is unknown: infer from Aspire logs or probe known ports.
- If the observed behavior differs from the expected behavior: keep the run visible and describe the mismatch instead of masking it.

## Quality Checks
- noVNC page is reachable before starting automation.
- The user is told exactly where to watch.
- Operation is visible at human speed, not instant headless execution.
- Any limitations or fallbacks used are explicitly disclosed.

## Completion Criteria
- User could watch a visible browser session perform the requested operation.
- Result summary describes what was shown on screen, including any incorrect behavior that appeared.
