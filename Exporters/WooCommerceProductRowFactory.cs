using System.Globalization;

public sealed class WooCommerceProductRowFactory
{
    private const int Id = 0;
    private const int Type = 1;
    private const int Sku = 2;
    private const int Name = 3;
    private const int Published = 4;
    private const int InStock = 13;
    private const int Stock = 14;
    private const int RegularPrice = 25;
    private const int Categories = 26;
    private const int Images = 29;
    private const int Parent = 32;
    private const int Attribute1Name = 39;
    private const int Attribute1Values = 40;
    private const int Attribute1Visible = 41;
    private const int Attribute1Global = 42;
    private const int Attribute2Name = 44;
    private const int Attribute2Values = 45;
    private const int Attribute2Visible = 46;
    private const int Attribute2Global = 47;

    public IReadOnlyList<string?> CreateParentProductRow(
        Product product,
        IEnumerable<ProductVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(variants);

        var discoveredVariants = variants.ToList();
        var row = CreateEmptyRow();

        row[Id] = product.Id.ToString(CultureInfo.InvariantCulture);
        row[Type] = "variable";
        row[Sku] = product.Sku;
        row[Name] = product.Name;
        row[Published] = "1";
        row[InStock] = discoveredVariants.Any(variant => variant.Available > 0) ? "1" : "0";
        row[Categories] = product.Category;
        row[Images] = string.Join(", ", GetProductImages(product));

        SetAttribute(
            row,
            Attribute1Name,
            Attribute1Values,
            Attribute1Visible,
            Attribute1Global,
            "Color",
            GetDistinctValues(discoveredVariants.Select(variant => variant.Color)));

        SetAttribute(
            row,
            Attribute2Name,
            Attribute2Values,
            Attribute2Visible,
            Attribute2Global,
            "Size",
            GetDistinctValues(discoveredVariants.Select(variant => variant.Size)));

        WooCommerceTxtExporter.ValidateColumnCount(row);
        return row;
    }

    public IReadOnlyList<string?> CreateVariationRow(
        ProductVariant variant,
        Product parent)
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentNullException.ThrowIfNull(parent);

        var row = CreateEmptyRow();

        row[Id] = variant.Id.ToString(CultureInfo.InvariantCulture);
        row[Type] = "variation";
        row[Sku] = variant.Sku;
        row[Name] = variant.Name;
        row[Published] = "1";
        row[InStock] = variant.Available > 0 ? "1" : "0";
        row[Stock] = variant.Available.ToString(CultureInfo.InvariantCulture);
        row[RegularPrice] = variant.Price.ToString(CultureInfo.InvariantCulture);
        row[Parent] = string.IsNullOrWhiteSpace(parent.Sku)
            ? $"id:{parent.Id.ToString(CultureInfo.InvariantCulture)}"
            : parent.Sku;

        SetAttribute(
            row,
            Attribute1Name,
            Attribute1Values,
            Attribute1Visible,
            Attribute1Global,
            "Color",
            GetDistinctValues([variant.Color]));

        SetAttribute(
            row,
            Attribute2Name,
            Attribute2Values,
            Attribute2Visible,
            Attribute2Global,
            "Size",
            GetDistinctValues([variant.Size]));

        WooCommerceTxtExporter.ValidateColumnCount(row);
        return row;
    }

    private static string?[] CreateEmptyRow() => new string?[WooCommerceTxtExporter.ColumnCount];

    private static IEnumerable<string> GetProductImages(Product product)
    {
        var images = product.Images
            .Append(product.Image)
            .Where(image => !string.IsNullOrWhiteSpace(image));

        return GetDistinctValues(images);
    }

    private static List<string> GetDistinctValues(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static void SetAttribute(
        string?[] row,
        int nameIndex,
        int valuesIndex,
        int visibleIndex,
        int globalIndex,
        string name,
        IReadOnlyCollection<string> values)
    {
        if (values.Count == 0)
            return;

        row[nameIndex] = name;
        row[valuesIndex] = string.Join(", ", values);
        row[visibleIndex] = "1";
        row[globalIndex] = "0";
    }
}
