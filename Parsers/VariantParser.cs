using System.Globalization;
using System.Text.Json;

public sealed class VariantParser
{
    public ProductVariant? Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object && TryGetProperty(root, "data", out var data))
            root = data;

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        var response = new ProductResponse
        {
            Id = GetInt64(root, "id"),
            ParentId = GetInt64(root, "parentId"),
            Code = GetString(root, "code"),
            Name = GetString(root, "name"),
            Price = GetDecimal(root, "price"),
            Available = GetInt32(root, "available"),
            Image = GetString(root, "image"),
            Images = GetImages(root)
        };

        if (response.Id == 0)
            return null;

        return new ProductVariant
        {
            Id = response.Id,
            ParentId = response.ParentId,
            Sku = response.Code,
            Name = response.Name,
            Price = response.Price,
            Available = response.Available,
            Image = response.Image,
            Images = response.Images
        };
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string GetString(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) ? value.ToString() : "";

    private static long GetInt64(JsonElement element, string name) =>
        long.TryParse(GetString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static int GetInt32(JsonElement element, string name) =>
        int.TryParse(GetString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static decimal GetDecimal(JsonElement element, string name) =>
        decimal.TryParse(GetString(element, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static List<string> GetImages(JsonElement element)
    {
        if (!TryGetProperty(element, "images", out var images) || images.ValueKind != JsonValueKind.Array)
            return [];

        return images.EnumerateArray()
            .Select(GetImageUrl)
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Select(image => image!)
            .ToList();
    }

    private static string? GetImageUrl(JsonElement image)
    {
        if (image.ValueKind == JsonValueKind.String)
            return image.GetString();

        if (image.ValueKind == JsonValueKind.Object)
        {
            var source = GetString(image, "src");
            return string.IsNullOrWhiteSpace(source) ? GetString(image, "thumb") : source;
        }

        return null;
    }
}
