
using Microsoft.Playwright;
using System.Text.RegularExpressions;

public class PlaywrightTest
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

        // Lắng nghe response từ website
        page.Response += async (_, response) =>
        {
            if (!response.Url.Contains("/product/child"))
                return;

            if (response.Request.Method != "POST")
                return;

            Console.WriteLine("\n========== API DETECTED ==========");

            Console.WriteLine($"URL: {response.Url}");
            Console.WriteLine($"Method: {response.Request.Method}");
            Console.WriteLine($"Status: {response.Status}");

            // Lấy request body
            var postData = response.Request.PostData ?? "";

            // Ẩn CSRF token trước khi in
            postData = Regex.Replace(
                postData,
                @"(userCommonCsrf=)[^&]*",
                "$1[REDACTED]"
            );

            Console.WriteLine("\n--- REQUEST BODY ---");
            Console.WriteLine(postData);

            // Lấy response JSON
            Console.WriteLine("\n--- RESPONSE BODY ---");

            try
            {
                var body = await response.TextAsync();
                Console.WriteLine(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot read response: {ex.Message}");
            }

            Console.WriteLine("==================================\n");
        };

        await page.GotoAsync(
            "https://mallow.vn/ga-vintage-soft-gap-1969-logo-baggy-wide-sweatpants-p39128355.html"
        );

        Console.WriteLine("Browser is ready!");
        Console.WriteLine("Select a color or size on the website.");

        // Giữ browser mở để thử thao tác
        await page.WaitForTimeoutAsync(60000);

        await browser.CloseAsync();
    }
}
