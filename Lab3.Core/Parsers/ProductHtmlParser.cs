
using AngleSharp.Dom;
using System.Globalization;
using System.Text.RegularExpressions;

public class ProductHtmlParser
{
    public List<Product> Parse(
        IDocument document,
        string categoryUrl,
        string categoryName)
    {
        var products = new List<Product>();

        var elements = document.QuerySelectorAll(
            ".productItems"
        );

        foreach (var element in elements)
        {
            var productLink = element.QuerySelector(
                ".product-img a[href]"
            );

            var nameElement = element.QuerySelector(
                ".tp_product_name"
            );

            var priceElement = element.QuerySelector(
                ".tp_product_price"
            );

            var oldPriceElement = element.QuerySelector(
                ".tp_product_price_old"
            );

            var idText = element.GetAttribute("data-id");

            if (!long.TryParse(idText, out var productId))
                continue;

            var href = productLink?.GetAttribute("href");

            if (string.IsNullOrWhiteSpace(href))
                continue;

            var productUrl = new Uri(
                new Uri(categoryUrl),
                href
            ).AbsoluteUri;

            var product = new Product
            {
                Id = productId,
                Name = nameElement?.TextContent.Trim() ?? "",
                Url = productUrl,
                Category = categoryName,

                Price = ParsePrice(
                    priceElement?.TextContent
                ),

                OriginalPrice = ParsePrice(
                    oldPriceElement?.TextContent
                ),

                Image = element.GetAttribute("data-img") ?? ""
            };

            products.Add(product);
        }

        return products;
    }

    private decimal? ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = Regex.Replace(value, @"[^\d]", "");

        if (decimal.TryParse(
            digits,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price))
        {
            return price;
        }

        return null;
    }
}
