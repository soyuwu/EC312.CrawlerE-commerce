public sealed class ProductOption
{
    public string Attribute { get; set; } = "";
    public string Value { get; set; } = "";

    public string OptionId { get; set; } = "";

    public string Column { get; set; } = "";
    public List<long> VariantIds { get; set; } = [];
}
