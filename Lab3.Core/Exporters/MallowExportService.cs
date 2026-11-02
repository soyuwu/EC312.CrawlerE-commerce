using Microsoft.Playwright;

public sealed record ExportSummary(
    int ParentProducts,
    int Variations,
    int SkippedProducts,
    int FailedVariants,
    int DuplicateVariants,
    string OutputPath);

public sealed class MallowExportService
{
    private readonly ProductCrawler _productCrawler = new();
    private readonly WooCommerceProductRowFactory _rowFactory = new();
    private readonly WooCommerceTxtExporter _csvExporter = new();
    private readonly MallowExportValidator _validator = new();

    public async Task<ExportSummary> ExportAsync(
        IEnumerable<Product> products,
        string outputPath = "mallow-products.csv",
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var productList = products.GroupBy(product => product.Id).Select(group => group.First()).ToList();
        var variantCache = new Dictionary<long, ProductVariant?>();
        var skipped = 0;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        for (var index = 0; index < productList.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var product = productList[index];
            progress?.Report(new CrawlProgress($"Crawling product {index + 1} of {productList.Count}: {product.Name}", index, productList.Count));
            try
            {
                product.Variants = await _productCrawler.CrawlVariantsAsync(page, product, variantCache, cancellationToken);
                if (product.Variants.Count == 0)
                {
                    skipped++;
                    Console.Error.WriteLine($"Skipped product {product.Id}: no variants were discovered.");
                }
            }
            catch (Exception ex) when (ex is PlaywrightException or HttpRequestException)
            {
                skipped++;
                product.Variants = [];
                Console.Error.WriteLine($"Skipped product {product.Id}: {ex.Message}");
            }
        }

        var exportableProducts = productList.Where(product => product.Variants.Count > 0).ToList();
        var duplicateVariants = exportableProducts.SelectMany(product => product.Variants)
            .GroupBy(variant => variant.Id).Count(group => group.Count() > 1);
        var validationErrors = _validator.Validate(exportableProducts);
        if (validationErrors.Count > 0)
            throw new InvalidOperationException("Export validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, validationErrors));

        var rows = new List<IReadOnlyList<string?>>();
        foreach (var product in exportableProducts)
        {
            rows.Add(_rowFactory.CreateParentProductRow(product, product.Variants));
            rows.AddRange(product.Variants.Select(variant => _rowFactory.CreateVariationRow(variant, product)));
        }

        await _csvExporter.WriteAsync(outputPath, rows);
        progress?.Report(new CrawlProgress("Export completed.", productList.Count, productList.Count));
        return new ExportSummary(
            exportableProducts.Count,
            exportableProducts.Sum(product => product.Variants.Count),
            skipped,
            variantCache.Count(pair => pair.Value is null),
            duplicateVariants,
            Path.GetFullPath(outputPath));
    }

    public static void PrintSummary(ExportSummary summary)
    {
        Console.WriteLine("========== EXPORT SUMMARY ==========");
        Console.WriteLine();
        Console.WriteLine($"Parent products:       {summary.ParentProducts}");
        Console.WriteLine($"Variations:            {summary.Variations}");
        Console.WriteLine($"Skipped products:      {summary.SkippedProducts}");
        Console.WriteLine($"Failed variants:       {summary.FailedVariants}");
        Console.WriteLine($"Duplicate variants:    {summary.DuplicateVariants}");
        Console.WriteLine();
        Console.WriteLine($"Output: {summary.OutputPath}");
        Console.WriteLine();
        Console.WriteLine("====================================");
    }
}
