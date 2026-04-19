using Microsoft.Playwright;
using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Adapters.Ui;

/// <summary>
/// Acceptance adapter that interacts with the application via the Playwright-driven browser UI.
/// Drives user interactions against the React frontend.
/// </summary>
public class PlaywrightAcceptanceAdapter : IAcceptanceAdapter
{
    private readonly IPage _page;
    private readonly IBrowserContext _context;
    private readonly IBrowser _browser;
    private readonly IPlaywright _playwrightInstance;

    private PlaywrightAcceptanceAdapter(IPage page, IBrowserContext context, IBrowser browser, IPlaywright playwright)
    {
        _page = page;
        _context = context;
        _browser = browser;
        _playwrightInstance = playwright;
    }

    /// <summary>
    /// Factory method to create and initialize a Playwright adapter with a browser session.
    /// </summary>
    public static async Task<PlaywrightAcceptanceAdapter> CreateAsync(string baseUri)
    {
        var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true // Run in headless mode for CI/unattended execution
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = baseUri
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        return new PlaywrightAcceptanceAdapter(page, context, browser, playwright);
    }

    public async Task<List<StoreItem>> GetStoresAsync()
    {
        // Wait for the store list to be present in the DOM
        await _page.WaitForSelectorAsync(".list");

        // Get all store rows and parse them
        var stores = new List<StoreItem>();
        var rows = await _page.QuerySelectorAllAsync(".list-row");

        foreach (var row in rows)
        {
            var nameEl = await row.QuerySelectorAsync(".store-name");
            var idEl = await row.QuerySelectorAsync(".store-id");

            if (nameEl != null && idEl != null)
            {
                var name = await nameEl.TextContentAsync() ?? "";
                var id = await idEl.TextContentAsync() ?? "";

                stores.Add(new StoreItem(id.Trim(), name.Trim(), []));
            }
        }

        return stores;
    }

    public async Task<CreateStoreResult> CreateStoreAsync(string id, string name)
    {
        // Clear any previous error messages
        await ClearFormErrorsAsync();

        // Find and fill the form inputs
        var idInput = await _page.QuerySelectorAsync("input[name=\"id\"]")
            ?? throw new InvalidOperationException("ID input field not found");
        var nameInput = await _page.QuerySelectorAsync("input[name=\"name\"]")
            ?? throw new InvalidOperationException("Name input field not found");

        await idInput.FillAsync(id);
        await nameInput.FillAsync(name);

        // Click the submit button
        var submitButton = await _page.QuerySelectorAsync("button:has-text('Add store')")
            ?? throw new InvalidOperationException("Submit button not found");
        await submitButton.ClickAsync();

        // Wait a moment for the response (either success or validation error)
        await _page.WaitForTimeoutAsync(500);

        // Check for validation errors first
        var idError = await GetFieldErrorAsync("id");
        var nameError = await GetFieldErrorAsync("name");
        var submitError = await _page.QuerySelectorAsync(".banner-error");

        if (!string.IsNullOrEmpty(idError) || !string.IsNullOrEmpty(nameError) || submitError != null)
        {
            var errors = new Dictionary<string, string[]>();
            if (!string.IsNullOrEmpty(idError))
                errors["Id"] = [idError];
            if (!string.IsNullOrEmpty(nameError))
                errors["Name"] = [nameError];

            var submitErrorText = submitError != null ? await submitError.TextContentAsync() : null;
            return new CreateStoreResult
            {
                Store = null,
                ValidationError = new ValidationErrorResult(
                    submitErrorText,
                    errors
                ),
                Success = false
            };
        }

        // Check if the form was cleared (success indicator for UI)
        var idInputValueAfter = await idInput.GetAttributeAsync("value");
        if (string.IsNullOrEmpty(idInputValueAfter))
        {
            // Form cleared = successful submission; the new store should now be in the list
            var stores = await GetStoresAsync();
            var created = stores.FirstOrDefault(l => l.Id == id);

            return new CreateStoreResult
            {
                Store = created,
                ValidationError = null,
                Success = created != null
            };
        }

        throw new InvalidOperationException("Unexpected state after form submission");
    }

    public async Task<int> GetStoreCountAsync()
    {
        // Look for the hero stat value element which displays the active stores count
        var countElement = await _page.QuerySelectorAsync(".hero-stat-value");
        if (countElement == null)
        {
            throw new InvalidOperationException("Store count element not found");
        }

        var countText = await countElement.TextContentAsync() ?? "0";
        if (int.TryParse(countText.Trim(), out int count))
        {
            return count;
        }

        throw new InvalidOperationException($"Could not parse store count: {countText}");
    }

    public async Task<StoreItem?> OpenStoreAsync(string id)
    {
        await _page.GotoAsync("/");

        // Scope lookup to visible store rows and click the row containing this store id.
        var storeRow = _page.Locator("a.list-row").Filter(new LocatorFilterOptions { HasTextString = id });
        await storeRow.First.WaitForAsync();
        await storeRow.First.ClickAsync();

        return await ReadSelectedStoreAsync();
    }

    public async Task<AddGroupResult> AddGroupToStoreAsync(string storeId, string groupName)
    {
        await _page.GotoAsync($"/stores/{Uri.EscapeDataString(storeId)}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByLabel("Group name").FillAsync(groupName);
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add group" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check for validation errors shown by the UI
        var groupNameErrorEl = await _page.QuerySelectorAsync("#group-name-error");
        var submitErrorEl = await _page.QuerySelectorAsync(".banner-error");

        if (groupNameErrorEl != null || submitErrorEl != null)
        {
            var errors = new Dictionary<string, string[]>();
            var nameError = groupNameErrorEl != null ? await groupNameErrorEl.TextContentAsync() : null;
            if (!string.IsNullOrEmpty(nameError))
                errors["Name"] = [nameError];

            var submitErrorText = submitErrorEl != null ? await submitErrorEl.TextContentAsync() : null;
            return new AddGroupResult
            {
                Store = null,
                ValidationError = new ValidationErrorResult(submitErrorText, errors),
                Success = false
            };
        }

        var store = await ReadSelectedStoreAsync();
        return new AddGroupResult
        {
            Store = store,
            ValidationError = null,
            Success = store != null
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _page.CloseAsync();
        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwrightInstance.Dispose();
    }

    private async Task ClearFormErrorsAsync()
    {
        // Click into the ID field to clear any errors (per App.tsx handleChange logic)
        var idInput = await _page.QuerySelectorAsync("input[name=\"id\"]");
        if (idInput != null)
        {
            await idInput.FocusAsync();
        }
    }

    private async Task<string?> GetFieldErrorAsync(string fieldName)
    {
        var errorId = fieldName == "id" ? "store-id-error" : "store-name-error";
        var errorElement = await _page.QuerySelectorAsync($"#{errorId}");

        if (errorElement == null)
            return null;

        return await errorElement.TextContentAsync();
    }

    private async Task<StoreItem?> ReadSelectedStoreAsync()
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForFunctionAsync(
            "() => { const element = document.querySelector('.store-page-name'); return element && element.textContent !== 'Loading store...'; }");

        var nameElement = await _page.QuerySelectorAsync(".store-page-name");
        var idElement = await _page.QuerySelectorAsync(".store-page-id");

        if (nameElement == null || idElement == null)
        {
            return null;
        }

        var name = (await nameElement.TextContentAsync())?.Trim();
        var id = (await idElement.TextContentAsync())?.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
        {
            return null;
        }

        var groups = new List<GroupItem>();
        var groupPills = _page.Locator(".group-pill");
        var groupCount = await groupPills.CountAsync();
        for (var i = 0; i < groupCount; i++)
        {
            var groupName = (await groupPills.Nth(i).TextContentAsync())?.Trim();
            if (!string.IsNullOrEmpty(groupName))
            {
                groups.Add(new GroupItem(groupName));
            }
        }

        return new StoreItem(id, name, groups);
    }
}
