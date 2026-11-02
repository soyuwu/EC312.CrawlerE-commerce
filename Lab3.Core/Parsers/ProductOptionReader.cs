
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

}
