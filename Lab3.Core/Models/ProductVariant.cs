
public class ProductVariant
{
    public long Id { get; set; }

    public long ParentId { get; set; }

    public string Sku { get; set; } = "";

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public int Available { get; set; }

    public string Image { get; set; } = "";

    public List<string> Images { get; set; } = [];

    public string Color { get; set; } = "";

    public string Size { get; set; } = "";
}

