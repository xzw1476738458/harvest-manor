using HarvestManor.Core.Content;
using HarvestManor.Core.Time;
using Godot;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private DayClock? _clock;
    private StaminaState? _stamina;

    public override void _Ready()
    {
        var (cropCount, itemCount) = LoadCatalogCounts(
            Godot.FileAccess.GetFileAsString("res://data/crops/spring.json"),
            Godot.FileAccess.GetFileAsString("res://data/items/items.json"));

        GD.Print($"Loaded {cropCount} crops and {itemCount} items.");

        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(maximum: 100, current: 100);

        GD.Print($"Day {_clock.Date.Day} of {_clock.Date.Season}, stamina {_stamina.Current}/{_stamina.Maximum}");
    }

    internal static (int CropCount, int ItemCount) LoadCatalogCounts(string cropCatalogJson, string itemCatalogJson)
    {
        var loader = new ContentCatalogLoader();
        var crops = loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        return (crops.Count, items.Count);
    }
}
