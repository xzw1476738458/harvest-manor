using HarvestManor.Core.Content;
using Godot;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    public override void _Ready()
    {
        var (cropCount, itemCount) = LoadCatalogCounts(
            Godot.FileAccess.GetFileAsString("res://data/crops/spring.json"),
            Godot.FileAccess.GetFileAsString("res://data/items/items.json"));

        GD.Print($"Loaded {cropCount} crops and {itemCount} items.");
    }

    internal static (int CropCount, int ItemCount) LoadCatalogCounts(string cropCatalogJson, string itemCatalogJson)
    {
        var loader = new ContentCatalogLoader();
        var crops = loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        return (crops.Count, items.Count);
    }
}
