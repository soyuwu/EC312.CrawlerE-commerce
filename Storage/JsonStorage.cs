
using System.Text.Json;

public class JsonStorage
{
    public async Task SaveAsync(
        List<Product> products,
        string fileName)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(
            products,
            options
        );

        await File.WriteAllTextAsync(fileName, json);
    }
}

