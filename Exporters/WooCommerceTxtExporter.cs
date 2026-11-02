using System.Text;

public sealed class WooCommerceTxtExporter
{
    public const int ColumnCount = 84;

    public static readonly IReadOnlyList<string> Header =
    [
        "ID", "Type", "SKU", "Name", "Published", "Is featured?", "Visibility in catalog",
        "Short description", "Description", "Date sale price starts", "Date sale price ends",
        "Tax status", "Tax class", "In stock?", "Stock", "Low stock amount",
        "Backorders allowed?", "Sold individually?", "Weight(kg)", "Length(cm)", "Width(cm)",
        "Height(cm)", "Allow customer reviews?", "Purchase note", "Sale price", "Regular price",
        "Categories", "Tags", "Shipping class", "Images", "Download limit", "Download expiry days",
        "Parent", "Grouped products", "Upsells", "Cross-sells", "External URL", "Button text",
        "Position", "Attribute 1 name", "Attribute 1 value(s)", "Attribute 1 visible",
        "Attribute 1 global", "Attribute 1 default", "Attribute 2 name", "Attribute 2 value(s)",
        "Attribute 2 visible", "Attribute 2 global", "Attribute 2 default", "Attribute 3 name",
        "Attribute 3 value(s)", "Attribute 3 visible", "Attribute 3 global", "Attribute 3 default",
        "Attribute 4 name", "Attribute 4 value(s)", "Attribute 4 visible", "Attribute 4 global",
        "Attribute 4 default", "Attribute 5 name", "Attribute 5 value(s)", "Attribute 5 visible",
        "Attribute 5 global", "Attribute 5 default", "Attribute 6 name", "Attribute 6 value(s)",
        "Attribute 6 visible", "Attribute 6 global", "Attribute 6 default", "Attribute 7 name",
        "Attribute 7 value(s)", "Attribute 7 visible", "Attribute 7 global", "Attribute 7 default",
        "Attribute 8 name", "Attribute 8 value(s)", "Attribute 8 visible", "Attribute 8 global",
        "Attribute 8 default", "Attribute 9 name", "Attribute 9 value(s)", "Attribute 9 visible",
        "Attribute 9 global", "Attribute 9 default"
    ];

    public async Task WriteAsync(string filePath, IEnumerable<IReadOnlyList<string?>> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(rows);

        await using var writer = new StreamWriter(
            filePath,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(CreateCsvLine(Header));

        foreach (var row in rows)
        {
            ValidateColumnCount(row);
            await writer.WriteLineAsync(CreateCsvLine(row));
        }
    }

    public static void ValidateColumnCount(IReadOnlyCollection<string?> row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.Count != ColumnCount)
        {
            throw new ArgumentException(
                $"WooCommerce CSV rows must contain exactly {ColumnCount} columns; received {row.Count}.",
                nameof(row));
        }
    }

    public static string CreateCsvLine(IEnumerable<string?> values) =>
        string.Join(',', values.Select(Escape));

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var requiresQuoting = value.IndexOfAny([',', '"', '\r', '\n']) >= 0;

        if (!requiresQuoting)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
