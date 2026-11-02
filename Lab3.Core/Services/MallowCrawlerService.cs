using System.Globalization;
using System.Text.RegularExpressions;

public sealed class MallowCrawlerService
{
    public async Task<ExportSummary> CrawlProductAsync(
        string productUrl,
        string outputDirectory,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var product = CreateProductFromUrl(productUrl);
        progress?.Report(new CrawlProgress("Preparing product crawl…", 0, 1));
        return await ExportAsync([product], outputDirectory, progress, cancellationToken);
    }

    public async Task<ExportSummary> CrawlCategoryAsync(
        string categoryUrl,
        string outputDirectory,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = ValidateCategoryUrl(categoryUrl);
        progress?.Report(new CrawlProgress("Discovering products in category…"));
        var products = await new CategoryCrawler().CrawlCategoryAsync(normalizedUrl, CreateCategoryName(normalizedUrl), cancellationToken);
        return await ExportAsync(products, outputDirectory, progress, cancellationToken);
    }

    public async Task<ExportSummary> CrawlCategoriesAsync(
        string categoryFile,
        string outputDirectory,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var categories = await ReadCategoriesAsync(categoryFile, cancellationToken);
        if (categories.Count == 0)
            throw new InvalidOperationException("The category file contains no valid categories.");

        var crawler = new CategoryCrawler();
        var products = new Dictionary<long, Product>();
        for (var index = 0; index < categories.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = categories[index];
            progress?.Report(new CrawlProgress($"Discovering: {category.Name}", index, categories.Count));
            var categoryProducts = await crawler.CrawlCategoryAsync(category.Url, category.Name, cancellationToken);
            foreach (var product in categoryProducts)
                products.TryAdd(product.Id, product);
        }

        return await ExportAsync(products.Values, outputDirectory, progress, cancellationToken);
    }

    private static async Task<ExportSummary> ExportAsync(
        IEnumerable<Product> products,
        string outputDirectory,
        IProgress<CrawlProgress>? progress,
        CancellationToken cancellationToken)
    {
        var productList = products.ToList();
        if (productList.Count == 0)
            throw new InvalidOperationException("No products were found.");

        var outputPath = EnsureOutputDirectory(outputDirectory);
        progress?.Report(new CrawlProgress("Saving discovered products…"));
        await new JsonStorage().SaveAsync(productList, Path.Combine(outputPath, "mallow-products.json"), cancellationToken);
        return await new MallowExportService().ExportAsync(productList, Path.Combine(outputPath, "mallow-products.csv"), progress, cancellationToken);
    }

    private static string EnsureOutputDirectory(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("An output folder is required.", nameof(outputDirectory));

        var fullPath = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    private static Product CreateProductFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("A valid absolute Mallow product URL is required.", nameof(url));

        var match = Regex.Match(url, @"-p(?<id>\d+)\.html(?:\?|$)", RegexOptions.IgnoreCase);
        if (!match.Success || !long.TryParse(match.Groups["id"].Value, out var id))
            throw new ArgumentException("The product URL must end with a Mallow product ID, such as -p39128355.html.", nameof(url));

        return new Product { Id = id, Url = url };
    }

    private static string ValidateCategoryUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.Host.Equals("mallow.vn", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A valid absolute Mallow category URL is required.", nameof(url));

        return uri.AbsoluteUri;
    }

    private static string CreateCategoryName(string categoryUrl)
    {
        var path = Uri.UnescapeDataString(new Uri(categoryUrl).AbsolutePath).Trim('/');
        var name = Regex.Replace(Path.GetFileNameWithoutExtension(path), @"[-_]+", " ").Trim();
        return string.IsNullOrWhiteSpace(name) ? "Mallow" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
    }

    private static async Task<List<(string Name, string Url)>> ReadCategoriesAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Category list file was not found.", path);

        var categories = new List<(string Name, string Url)>();
        foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !Uri.TryCreate(parts[1], UriKind.Absolute, out _))
                throw new InvalidOperationException($"Invalid category entry: {line}");
            categories.Add((parts[0], parts[1]));
        }
        return categories;
    }
}
