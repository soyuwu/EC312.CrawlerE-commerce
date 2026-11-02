
using Microsoft.Playwright;
using System.Text.Json;

public class ProductOptionReader
{
    public async Task<List<ProductOption>> ReadAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
            () => JSON.stringify([
                ...[...document.querySelectorAll('span.color[data-column] a')].map(element => ({
                    attribute: 'Color',
                    value: element.getAttribute('data-attrname') || element.getAttribute('title') || (element.textContent || '').trim(),
                    optionId: element.getAttribute('value') || '',
                    column: element.parentElement?.getAttribute('data-column') || '',
                    variantIds: (element.getAttribute('data-pids') || '').split(',').map(id => Number(id.trim())).filter(Number.isSafeInteger)
                })),
                ...[...document.querySelectorAll('span.size[data-column] a')].map(element => ({
                    attribute: 'Size',
                    value: (element.textContent || '').trim(),
                    optionId: element.getAttribute('value') || '',
                    column: element.parentElement?.getAttribute('data-column') || '',
                    variantIds: []
                }))
            ])
            """);

        var options = JsonSerializer.Deserialize<List<ProductOption>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        return options.Where(option => !string.IsNullOrWhiteSpace(option.OptionId)).ToList();
    }

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

        // Đọc danh sách màu
        var colors = await page.Locator("a.cloudzoom-gallery").EvaluateAllAsync<object>(
            """
            elements => elements.map(element => ({
                name: element.getAttribute('data-attrname'),
                optionId: element.getAttribute('value'),
                variantIds: element.getAttribute('data-pids')
                    ?.split(',')
                    .filter(id => id.length > 0)
            }))
            """
        );

        Console.WriteLine("========== COLORS ==========");

        Console.WriteLine(
            System.Text.Json.JsonSerializer.Serialize(
                colors,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }
            )
        );

        await page.WaitForTimeoutAsync(3000);

        await browser.CloseAsync();
    }
}
