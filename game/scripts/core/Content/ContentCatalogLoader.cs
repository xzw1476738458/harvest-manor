using System.Text.Json;

namespace HarvestManor.Core.Content;

public sealed class ContentCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<CropDefinition> LoadCropCatalog(string path)
    {
        using var stream = File.OpenRead(path);
        var crops = JsonSerializer.Deserialize<List<CropDefinition>>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Crop catalog '{path}' was empty.");

        foreach (var crop in crops)
        {
            crop.Validate();
        }

        return crops;
    }

    public IReadOnlyList<ItemDefinition> LoadItemCatalog(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<ItemDefinition>>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Item catalog '{path}' was empty.");
    }
}
