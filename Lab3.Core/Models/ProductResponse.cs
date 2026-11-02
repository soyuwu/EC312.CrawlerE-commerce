public sealed class ProductResponse
{
    public long Id { get; set; }
    public long ParentId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Available { get; set; }
    public string Image { get; set; } = "";
    public List<string> Images { get; set; } = [];
}
