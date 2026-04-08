using HarvestManor.Core.Content;
using Godot;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    public override void _Ready()
    {
        var (cropCount, itemCount) = LoadCatalogCounts(
            ProjectSettings.GlobalizePath("res://data/crops/spring.json"),
            ProjectSettings.GlobalizePath("res://data/items/items.json"));

        GD.Print($"Loaded {cropCount} crops and {itemCount} items.");
    }

    internal static (int CropCount, int ItemCount) LoadCatalogCounts(string cropCatalogPath, string itemCatalogPath)
    {
        var loader = new ContentCatalogLoader();
        var crops = loader.LoadCropCatalog(cropCatalogPath);
        var items = loader.LoadItemCatalog(itemCatalogPath);
        return (crops.Count, items.Count);
    }
}
