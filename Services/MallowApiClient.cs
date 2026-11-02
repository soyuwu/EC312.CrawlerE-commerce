
using Microsoft.Playwright;

public sealed class MallowApiClient
{
    private readonly VariantParser _variantParser = new();

    public async Task<ProductVariant?> GetVariantAsync(
        IAPIRequestContext request,
        long variantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://mallow.vn/product/child?childId={variantId}";
        var response = await request.PostAsync(url, new APIRequestContextOptions
        {
            FailOnStatusCode = false
        });

        try
        {
            if (!response.Ok)
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            return _variantParser.Parse(await response.TextAsync());
        }
        finally
        {
            await response.DisposeAsync();
        }
    }

    public async Task<ProductVariant?> GetVariantForOptionsAsync(
        IAPIRequestContext request,
        long parentId,
        IEnumerable<ProductOption> options,
        CancellationToken cancellationToken = default)
    {
        var optionList = options
            .Where(option => !string.IsNullOrWhiteSpace(option.Column) && !string.IsNullOrWhiteSpace(option.OptionId))
            .ToList();

        if (optionList.Count == 0)
            return null;

        var form = request.CreateFormData();
        foreach (var option in optionList)
            form.Set($"attrs[{option.Column}]", option.OptionId);

        var response = await request.PostAsync($"https://mallow.vn/product/child?psId={parentId}", new APIRequestContextOptions
        {
            Form = form,
            FailOnStatusCode = false
        });

        try
        {
            if (!response.Ok)
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            return _variantParser.Parse(await response.TextAsync());
        }
        finally
        {
            await response.DisposeAsync();
        }
    }
}
