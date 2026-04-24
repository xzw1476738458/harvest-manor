using System.Text.Json;

namespace HarvestManor.Core.Content;

public sealed class ContentCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.ReadOptions;

    public IReadOnlyList<CropDefinition> LoadCropCatalog(string path)
    {
        var json = File.ReadAllText(path);
        return ParseCropCatalogJson(json, path);
    }

    public IReadOnlyList<CropDefinition> ParseCropCatalogJson(string json, string sourceName = "inline")
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"Crop catalog '{sourceName}' was empty.");
        }

        var crops = JsonSerializer.Deserialize<List<CropDefinition>>(json, JsonOptions)
            ?? throw new InvalidDataException($"Crop catalog '{sourceName}' was empty.");

        foreach (var crop in crops)
        {
            crop.Validate();
        }

        return crops;
    }

    public IReadOnlyList<ItemDefinition> LoadItemCatalog(string path)
    {
        var json = File.ReadAllText(path);
        return ParseItemCatalogJson(json, path);
    }

    public IReadOnlyList<ItemDefinition> ParseItemCatalogJson(string json, string sourceName = "inline")
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"Item catalog '{sourceName}' was empty.");
        }

        var items = JsonSerializer.Deserialize<List<ItemDefinition>>(json, JsonOptions)
            ?? throw new InvalidDataException($"Item catalog '{sourceName}' was empty.");

        foreach (var item in items)
        {
            item.Validate();
        }

        return items;
    }
}
