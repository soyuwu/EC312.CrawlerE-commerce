
using Microsoft.Playwright;

public sealed class ProductCrawler
{
    private readonly ProductOptionReader _optionReader = new();
    private readonly MallowApiClient _apiClient = new();

    public async Task<List<ProductVariant>> CrawlVariantsAsync(
        IPage page,
        Product product,
        IDictionary<long, ProductVariant?> variantCache,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(product);

        await page.GotoAsync(product.Url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PopulateProductDetailsAsync(page, product);
        var options = await _optionReader.ReadAsync(page);
        var sizeByVariantId = await BuildSizeMapAsync(page.Context.APIRequest, product.Id, options, variantCache, cancellationToken);
        var optionByVariantId = options
            .SelectMany(option => option.VariantIds.Select(id => new { id, option }))
            .GroupBy(entry => entry.id)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.option).ToList());

        var variants = new List<ProductVariant>();
        foreach (var variantId in optionByVariantId.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!variantCache.TryGetValue(variantId, out var variant))
            {
                try
                {
                    variant = await _apiClient.GetVariantAsync(page.Context.APIRequest, variantId, cancellationToken);
                    variantCache[variantId] = variant;
                    await Task.Delay(300, cancellationToken);
                }
                catch (Exception ex) when (ex is PlaywrightException or HttpRequestException)
                {
                    Console.Error.WriteLine($"Failed variant {variantId}: {ex.Message}");
                    variantCache[variantId] = null;
                    continue;
                }
            }

            if (variant is null)
                continue;

            var copy = CopyVariant(variant);
            copy.ParentId = copy.ParentId == 0 ? product.Id : copy.ParentId;
            ApplyOptions(copy, optionByVariantId[variantId]);
            if (sizeByVariantId.TryGetValue(copy.Id, out var size))
                copy.Size = size;
            variants.Add(copy);
        }

        return variants;
    }

    private async Task<Dictionary<long, string>> BuildSizeMapAsync(
        IAPIRequestContext request,
        long parentId,
        IReadOnlyCollection<ProductOption> options,
        IDictionary<long, ProductVariant?> variantCache,
        CancellationToken cancellationToken)
    {
        var colors = options.Where(option => option.Attribute.Equals("Color", StringComparison.OrdinalIgnoreCase) && option.VariantIds.Count > 0).ToList();
        var sizes = options.Where(option => option.Attribute.Equals("Size", StringComparison.OrdinalIgnoreCase)).ToList();
        var sizeByVariantId = new Dictionary<long, string>();

        foreach (var color in colors)
        {
            foreach (var size in sizes)
            {
                try
                {
                    var variant = await _apiClient.GetVariantForOptionsAsync(request, parentId, [color, size], cancellationToken);
                    if (variant is null || !color.VariantIds.Contains(variant.Id))
                        continue;

                    variantCache.TryAdd(variant.Id, variant);
                    sizeByVariantId[variant.Id] = size.Value;
                    await Task.Delay(150, cancellationToken);
                }
                catch (Exception ex) when (ex is PlaywrightException or HttpRequestException)
                {
                    Console.Error.WriteLine($"Failed size mapping for product {parentId}: {ex.Message}");
                }
            }
        }

        return sizeByVariantId;
    }

    private static async Task PopulateProductDetailsAsync(IPage page, Product product)
    {
        var details = await page.EvaluateAsync<string[]>(
            """
            () => {
                const sku = document.querySelector('[itemprop="sku"], [data-sku], .product-code, #productCode')?.getAttribute('content')
                    || document.querySelector('[itemprop="sku"], [data-sku], .product-code, #productCode')?.textContent?.trim()
                    || '';
                const name = document.querySelector('h1')?.textContent?.trim() || '';
                const images = [...document.querySelectorAll('meta[property="og:image"], img[data-zoom-image]')]
                    .map(element => element.getAttribute('content') || element.getAttribute('data-zoom-image') || '')
                    .filter(Boolean);
                return [sku, name, ...images];
            }
            """);

        if (string.IsNullOrWhiteSpace(product.Sku) && details.Length > 0)
            product.Sku = details[0];
        if (string.IsNullOrWhiteSpace(product.Name) && details.Length > 1)
            product.Name = details[1];
        product.Images = product.Images.Concat(details.Skip(2)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void ApplyOptions(ProductVariant variant, IEnumerable<ProductOption> options)
    {
        foreach (var option in options)
        {
            var attribute = option.Attribute.Trim();
            if (attribute.Equals("Color", StringComparison.OrdinalIgnoreCase) || attribute.Contains("màu", StringComparison.OrdinalIgnoreCase))
                variant.Color = option.Value;
            else if (attribute.Equals("Size", StringComparison.OrdinalIgnoreCase) || attribute.Contains("kích", StringComparison.OrdinalIgnoreCase))
                variant.Size = option.Value;
        }
    }

    private static ProductVariant CopyVariant(ProductVariant source) => new()
    {
        Id = source.Id, ParentId = source.ParentId, Sku = source.Sku, Name = source.Name,
        Price = source.Price, Available = source.Available, Image = source.Image,
        Images = [.. source.Images], Color = source.Color, Size = source.Size
    };
}
