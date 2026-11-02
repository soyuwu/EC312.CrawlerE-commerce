
public class Product
{
    public long Id { get; set; }

    public string Name { get; set; } = "";

    public string Sku { get; set; } = "";

    public string Url { get; set; } = "";

    public string Category { get; set; } = "";

    public decimal? Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public string Image { get; set; } = "";

    public List<string> Images { get; set; } = [];

    public List<ProductVariant> Variants { get; set; } = [];
}
