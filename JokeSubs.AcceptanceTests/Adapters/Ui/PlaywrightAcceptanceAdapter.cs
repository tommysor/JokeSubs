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
    private readonly IBrowser _browser;
    private readonly IPlaywright _playwrightInstance;

    public AdapterKind Kind => AdapterKind.Ui;

    private PlaywrightAcceptanceAdapter(IPage page, IBrowser browser, IPlaywright playwright)
    {
        _page = page;
        _browser = browser;
        _playwrightInstance = playwright;
    }

    /// <summary>
    /// Factory method to create and initialize a Playwright adapter with a browser session.
    /// </summary>
    public static async Task<PlaywrightAcceptanceAdapter> CreateAsync(AspireAssemblyFixture fixture)
    {
        var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true // Run in headless mode for CI/unattended execution
        });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(fixture.UiBaseUri.ToString());

        return new PlaywrightAcceptanceAdapter(page, browser, playwright);
    }

    public async Task<List<LocationItem>> GetLocationsAsync()
    {
        // Wait for the location list to be present in the DOM
        await _page.WaitForSelectorAsync(".list");

        // Get all location rows and parse them
        var locations = new List<LocationItem>();
        var rows = await _page.QuerySelectorAllAsync(".list-row");

        foreach (var row in rows)
        {
            var nameEl = await row.QuerySelectorAsync(".location-name");
            var idEl = await row.QuerySelectorAsync(".location-id");

            if (nameEl != null && idEl != null)
            {
                var name = await nameEl.TextContentAsync() ?? "";
                var id = await idEl.TextContentAsync() ?? "";

                locations.Add(new LocationItem(id.Trim(), name.Trim()));
            }
        }

        return locations;
    }

    public async Task<CreateLocationResult> CreateLocationAsync(string id, string name)
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
        var submitButton = await _page.QuerySelectorAsync("button:has-text('Add location')")
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
            return new CreateLocationResult
            {
                Location = null,
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
            // Form cleared = successful submission; the new location should now be in the list
            var locations = await GetLocationsAsync();
            var created = locations.FirstOrDefault(l => l.Id == id);

            return new CreateLocationResult
            {
                Location = created,
                ValidationError = null,
                Success = created != null
            };
        }

        throw new InvalidOperationException("Unexpected state after form submission");
    }

    public async Task<int> GetLocationCountAsync()
    {
        // Look for the hero stat value element which displays the active locations count
        var countElement = await _page.QuerySelectorAsync(".hero-stat-value");
        if (countElement == null)
        {
            throw new InvalidOperationException("Location count element not found");
        }

        var countText = await countElement.TextContentAsync() ?? "0";
        if (int.TryParse(countText.Trim(), out int count))
        {
            return count;
        }

        throw new InvalidOperationException($"Could not parse location count: {countText}");
    }

    public async Task DisposeAsync()
    {
        await _page.CloseAsync();
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
        var errorId = fieldName == "id" ? "location-id-error" : "location-name-error";
        var errorElement = await _page.QuerySelectorAsync($"#{errorId}");

        if (errorElement == null)
            return null;

        return await errorElement.TextContentAsync();
    }
}
