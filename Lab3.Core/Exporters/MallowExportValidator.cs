using System.Globalization;

public sealed class MallowExportValidator
{
    public IReadOnlyList<string> Validate(IEnumerable<Product> products)
    {
        var productList = products.ToList();
        var errors = new List<string>();
        AddDuplicates(errors, productList.Select(product => product.Id), "parent product");

        var parents = productList.ToDictionary(product => product.Id);
        var variants = productList.SelectMany(product => product.Variants).ToList();
        AddDuplicates(errors, variants.Select(variant => variant.Id), "variant");

        foreach (var product in productList)
        {
            if (product.Variants.Count == 0)
                errors.Add($"Product {product.Id} has no successfully discovered variants.");

            ValidateImages(errors, product.Id.ToString(CultureInfo.InvariantCulture), product.Images.Append(product.Image));
        }

        foreach (var variant in variants)
        {
            if (!parents.TryGetValue(variant.ParentId, out var parent))
                errors.Add($"Variant {variant.Id} references missing parent {variant.ParentId}.");
            if (variant.Available < 0)
                errors.Add($"Variant {variant.Id} has negative stock ({variant.Available}).");
            if (string.IsNullOrWhiteSpace(variant.Sku))
                errors.Add($"Variant {variant.Id} has no SKU from Mallow.");

            ValidateImages(errors, variant.Id.ToString(CultureInfo.InvariantCulture), variant.Images.Append(variant.Image));
        }

        return errors;
    }

    private static void AddDuplicates(List<string> errors, IEnumerable<long> ids, string entityName)
    {
        foreach (var duplicate in ids.GroupBy(id => id).Where(group => group.Count() > 1))
            errors.Add($"Duplicate {entityName} ID: {duplicate.Key}.");
    }

    private static void ValidateImages(List<string> errors, string owner, IEnumerable<string> images)
    {
        foreach (var image in images.Where(image => !string.IsNullOrWhiteSpace(image)))
        {
            if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add($"{owner} has an invalid image URL: {image}.");
            }
        }
    }
}
