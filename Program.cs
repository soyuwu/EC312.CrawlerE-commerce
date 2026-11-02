var service = new MallowCrawlerService();
var outputDirectory = Path.Combine(Environment.CurrentDirectory, "output");

try
{
    ExportSummary? summary = null;
    if (args.Length == 2 && args[0].Equals("--product", StringComparison.OrdinalIgnoreCase))
        summary = await service.CrawlProductAsync(args[1], outputDirectory);
    else if (args.Length == 2 && args[0].Equals("--category", StringComparison.OrdinalIgnoreCase))
        summary = await service.CrawlCategoryAsync(args[1], outputDirectory);
    else if (args.Length > 0 && args[0].Equals("--categories", StringComparison.OrdinalIgnoreCase))
        summary = await service.CrawlCategoriesAsync(args.Length > 1 ? args[1] : Path.Combine("Data", "product-urls.txt"), outputDirectory);

    if (summary is not null)
    {
        MallowExportService.PrintSummary(summary);
        return;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine("Usage:");
Console.WriteLine("  dotnet run -- --product <mallow-product-url>");
Console.WriteLine("  dotnet run -- --category <mallow-category-url>");
Console.WriteLine("  dotnet run -- --categories [Data/product-urls.txt]");
Console.WriteLine("Output is written to the output folder.");
