
using System.Globalization;
using System.Text.RegularExpressions;

if (args.Length == 2 && args[0].Equals("--product", StringComparison.OrdinalIgnoreCase))
{
    var product = CreateProductFromUrl(args[1]);
    var summary = await new MallowExportService().ExportAsync([product]);
    MallowExportService.PrintSummary(summary);
    return;
}

if (args.Length == 2 && args[0].Equals("--category", StringComparison.OrdinalIgnoreCase))
{
    var categoryUrl = ValidateCategoryUrl(args[1]);
    var categoryName = CreateCategoryName(categoryUrl);
    var products = await new CategoryCrawler().CrawlCategoryAsync(categoryUrl, categoryName);

    if (products.Count == 0)
        throw new InvalidOperationException($"No products were found for category: {categoryUrl}");

    await new JsonStorage().SaveAsync(products, "mallow-products.json");
    var summary = await new MallowExportService().ExportAsync(products);
    MallowExportService.PrintSummary(summary);
    return;
}

if (args.Length > 0 && args[0].Equals("--categories", StringComparison.OrdinalIgnoreCase))
{
    var categoryFile = args.Length > 1 ? args[1] : Path.Combine("Data", "product-urls.txt");
    var categories = await ReadCategoriesAsync(categoryFile);
    if (categories.Count == 0)
        throw new InvalidOperationException($"No categories found in {categoryFile}. Use one line per category in the format: Category name|https://mallow.vn/category.html");

    var crawler = new CategoryCrawler();
    var products = new Dictionary<long, Product>();
    foreach (var category in categories)
    {
        var categoryProducts = await crawler.CrawlCategoryAsync(category.Url, category.Name);
        foreach (var product in categoryProducts)
            products.TryAdd(product.Id, product);
    }

    await new JsonStorage().SaveAsync(products.Values.ToList(), "mallow-products.json");
    var summary = await new MallowExportService().ExportAsync(products.Values);
    MallowExportService.PrintSummary(summary);
    return;
}

if (args.Length == 1 && args[0].Equals("--read-options", StringComparison.OrdinalIgnoreCase))
{
    await ProductOptionReader.RunAsync();
    return;
}

if (args.Length == 1 && args[0].Equals("--variant-api", StringComparison.OrdinalIgnoreCase))
{
    await VariantApiClient.RunAsync();
    return;
}

Console.WriteLine("Usage:");
Console.WriteLine("  dotnet run -- --product <mallow-product-url>");
Console.WriteLine("  dotnet run -- --category <mallow-category-url>");
Console.WriteLine("  dotnet run -- --categories [Data/product-urls.txt]");
Console.WriteLine("Category file format: Category name|https://mallow.vn/category.html");

static Product CreateProductFromUrl(string url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        throw new ArgumentException("A valid absolute Mallow product URL is required.", nameof(url));

    var match = Regex.Match(url, @"-p(?<id>\d+)\.html(?:\?|$)", RegexOptions.IgnoreCase);
    if (!match.Success || !long.TryParse(match.Groups["id"].Value, out var id))
        throw new ArgumentException("The product URL must end with its Mallow product ID, such as -p39128355.html.", nameof(url));

    return new Product { Id = id, Url = url };
}

static string ValidateCategoryUrl(string url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
        || !uri.Host.Equals("mallow.vn", StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("A valid absolute Mallow category URL is required.", nameof(url));
    }

    return uri.AbsoluteUri;
}

static string CreateCategoryName(string categoryUrl)
{
    var path = Uri.UnescapeDataString(new Uri(categoryUrl).AbsolutePath).Trim('/');
    var slug = Path.GetFileNameWithoutExtension(path);
    var name = Regex.Replace(slug, @"[-_]+", " ").Trim();

    return string.IsNullOrWhiteSpace(name)
        ? "Mallow"
        : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
}

static async Task<List<(string Name, string Url)>> ReadCategoriesAsync(string path)
{
    var categories = new List<(string Name, string Url)>();
    foreach (var line in await File.ReadAllLinesAsync(path))
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
