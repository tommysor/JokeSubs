# Repository Instructions

- Use the existing Aspire skill and workspace guidance for app orchestration. Keep Aspire-specific operational guidance there rather than duplicating it in this file.
- Acceptance test source of truth lives in `JokeSubs.AcceptanceTests/README.md` and `JokeSubs.AcceptanceTests/ARCHITECTURE.md`.
- Acceptance tests model product features, not transports. The API and UI are different ways to use the same feature.
- Default to one `[Theory]` with `AllAdaptersData` for a feature scenario. Use `ApiOnlyData` or `UiOnlyData` only when the behavior itself is transport-specific.
- Keep DSL and adapter contracts intent-based. Prefer names like `WhenOpeningStoreAsync` and `OpenStoreAsync` over transport-specific names.
- In Playwright acceptance tests, prefer visible text and labels first, scope locators to the relevant UI region or row, and avoid hidden-attribute coupling when a user-visible selector is available.