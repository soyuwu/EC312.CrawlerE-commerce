
using AngleSharp;
using AngleSharp.Dom;

public class CategoryCrawler
{
    private readonly HttpClient _httpClient;
    private readonly ProductHtmlParser _parser;

    public CategoryCrawler()
    {
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 Chrome/156.0.0.0 Safari/537.36"
        );

        _parser = new ProductHtmlParser();
    }

    public async Task<List<Product>> CrawlCategoryAsync(
        string categoryUrl,
        string categoryName)
    {
        var products = new Dictionary<long, Product>();

        for (int page = 1; ; page++)
        {
            var url = BuildPageUrl(categoryUrl, page);

            Console.WriteLine($"Crawling: {url}");

            var html = await _httpClient.GetStringAsync(url);

            var context = BrowsingContext.New(
                Configuration.Default
            );

            var document = await context.OpenAsync(
                request => request.Content(html)
            );

            var pageProducts = _parser.Parse(
                document,
                url,
                categoryName
            );

            Console.WriteLine(
                $"Found {pageProducts.Count} products."
            );

            int newProducts = 0;

            foreach (var product in pageProducts)
            {
                if (products.TryAdd(product.Id, product))
                    newProducts++;
            }

            if (pageProducts.Count == 0 || newProducts == 0)
                break;

            await Task.Delay(500);
        }

        return products.Values.ToList();
    }

    private string BuildPageUrl(string categoryUrl, int page)
    {
        var uri = new UriBuilder(categoryUrl);

        var query = System.Web.HttpUtility.ParseQueryString(
            uri.Query
        );

        if (page == 1)
            query.Remove("page");
        else
            query["page"] = page.ToString();

        uri.Query = query.ToString();

        return uri.Uri.AbsoluteUri;
    }
}
