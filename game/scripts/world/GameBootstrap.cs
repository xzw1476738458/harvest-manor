using System.Linq;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private readonly ContentCatalogLoader _loader = new();

    private CropGrowthService? _growth;
    private DayClock? _clock;
    private StaminaState? _stamina;
    private Wallet? _wallet;
    private InventoryState? _inventory;
    private FarmGrid? _farmGrid;
    private HudController? _hud;

    public override void _Ready()
    {
        var cropCatalogJson = Godot.FileAccess.GetFileAsString("res://data/crops/spring.json");
        var itemCatalogJson = Godot.FileAccess.GetFileAsString("res://data/items/items.json");

        var crops = _loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = _loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        var cropCatalog = crops.ToDictionary(crop => crop.Id);

        _growth = new CropGrowthService(cropCatalog);
        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(maximum: 100, current: 100);
        _wallet = new Wallet(200);
        _inventory = new InventoryState(12, 99);
        _farmGrid = new FarmGrid(6, 6);

        var starterCrop = crops.FirstOrDefault();
        if (starterCrop is not null)
        {
            _farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant(starterCrop.Id).Water());
        }

        var farmScene = GD.Load<PackedScene>("res://scenes/world/FarmScene.tscn").Instantiate<Node2D>();
        AddChild(farmScene);
        WireFarmScene(farmScene);

        _hud = GD.Load<PackedScene>("res://scenes/ui/Hud.tscn").Instantiate<HudController>();
        AddChild(_hud);

        GD.Print($"Loaded {crops.Count} crops and {items.Count} items.");
        GD.Print($"Day {_clock.Date.Day} of {_clock.Date.Season}, stamina {_stamina.Current}/{_stamina.Maximum}");

        RefreshHud();
    }

    internal static (int CropCount, int ItemCount) LoadCatalogCounts(string cropCatalogJson, string itemCatalogJson)
    {
        var loader = new ContentCatalogLoader();
        var crops = loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        return (crops.Count, items.Count);
    }

    public static bool ProcessDayEnd(
        DayClock clock,
        StaminaState stamina,
        CropGrowthService growth,
        FarmGrid farmGrid,
        int minutesToAdvance = 20 * 60)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(stamina);
        ArgumentNullException.ThrowIfNull(growth);
        ArgumentNullException.ThrowIfNull(farmGrid);

        var rolled = clock.AdvanceMinutes(minutesToAdvance);
        if (!rolled)
        {
            return false;
        }

        foreach (var plot in farmGrid.AllPlots.ToList())
        {
            farmGrid.SetPlot(growth.AdvanceDay(plot));
        }

        stamina.RestoreFull();
        return rolled;
    }

    private void WireFarmScene(Node farmScene)
    {
        var bed = farmScene.GetNodeOrNull<BedInteraction>("Bed");
        if (bed is null)
        {
            GD.PushWarning("Farm scene is missing a BedInteraction node named 'Bed'.");
            return;
        }

        bed.DayEndRequested += OnDayEndRequested;
    }

    private void OnDayEndRequested()
    {
        EndDay();
    }

    private void EndDay()
    {
        if (_clock is null || _stamina is null || _growth is null || _farmGrid is null)
        {
            return;
        }

        var rolled = ProcessDayEnd(_clock, _stamina, _growth, _farmGrid);
        if (rolled)
        {
            RefreshHud();
        }
    }

    private void RefreshHud()
    {
        if (_clock is null || _stamina is null || _wallet is null || _hud is null)
        {
            return;
        }

        _hud.SetDay($"Day {_clock.Date.Day} ({_clock.Date.Season})");
        _hud.SetGold(_wallet.Gold);
        _hud.SetStamina(_stamina.Current, _stamina.Maximum);
    }
}
