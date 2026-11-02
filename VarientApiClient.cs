
using Microsoft.Playwright;

public class VariantApiClient
{
    public static async Task RunAsync()
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = false
            }
        );

        var page = await browser.NewPageAsync();

        await page.GotoAsync(
            "https://mallow.vn/ga-vintage-soft-gap-1969-logo-baggy-wide-sweatpants-p39128355.html"
        );

        // Test với Variant ID đã biết
        long variantId = 39128369;

        var url = $"https://mallow.vn/product/child?childId={variantId}";

        var response = await page.Context.APIRequest.PostAsync(url);

        Console.WriteLine($"Status: {response.Status}");

        var json = await response.TextAsync();

        Console.WriteLine("Response:");
        Console.WriteLine(json);

        await browser.CloseAsync();
    }
}
