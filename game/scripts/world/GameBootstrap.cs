using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ContentCatalogLoader _loader = new();
    private readonly RequestBoardService _requestBoardService = new();
    private readonly FarmExpansionService _expansionService = new();
    private readonly HashSet<string> _completedRequestIds = new();
    private readonly UnlockState _unlockState = new(new HashSet<string> { "0,0", "1,0", "0,1", "1,1" });

    private CropGrowthService? _growth;
    private DayClock? _clock;
    private StaminaState? _stamina;
    private Wallet? _wallet;
    private InventoryState? _inventory;
    private InventoryState? _storage;
    private FarmGrid? _farmGrid;
    private HudController? _hud;
    private InventoryPanelController? _inventoryPanel;
    private ShopPanelController? _shopPanel;
    private StoragePanelController? _storagePanel;
    private IReadOnlyList<ShopOffer> _shopOffers = Array.Empty<ShopOffer>();
    private IReadOnlyList<RequestDefinition> _requests = Array.Empty<RequestDefinition>();

    public override void _Ready()
    {
        var cropCatalogJson = Godot.FileAccess.GetFileAsString("res://data/crops/spring.json");
        var itemCatalogJson = Godot.FileAccess.GetFileAsString("res://data/items/items.json");
        var shopCatalogJson = Godot.FileAccess.GetFileAsString("res://data/shops/general-store.json");
        var requestCatalogJson = Godot.FileAccess.GetFileAsString("res://data/requests/request-board.json");

        var crops = _loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = _loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        _shopOffers = DeserializeList<ShopOffer>(shopCatalogJson, "res://data/shops/general-store.json");
        _requests = DeserializeList<RequestDefinition>(requestCatalogJson, "res://data/requests/request-board.json");
        var cropCatalog = crops.ToDictionary(crop => crop.Id);

        _growth = new CropGrowthService(cropCatalog);
        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(maximum: 100, current: 100);
        _wallet = new Wallet(200);
        _inventory = new InventoryState(12, 99);
        _storage = new InventoryState(24, 99);
        _farmGrid = new FarmGrid(6, 6);
        _inventory.TryAdd("parsnip_seed", 4);
        _storage.TryAdd("wood", 12);

        var starterCrop = crops.FirstOrDefault();
        if (starterCrop is not null)
        {
            _farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant(starterCrop.Id).Water());
        }

        var farmScene = GD.Load<PackedScene>("res://scenes/world/FarmScene.tscn").Instantiate<Node2D>();
        AddChild(farmScene);
        WireFarmScene(farmScene);

        var townScene = GD.Load<PackedScene>("res://scenes/world/TownScene.tscn").Instantiate<Node2D>();
        AddChild(townScene);
        WireTownScene(townScene);

        _hud = GD.Load<PackedScene>("res://scenes/ui/Hud.tscn").Instantiate<HudController>();
        AddChild(_hud);

        _inventoryPanel = GD.Load<PackedScene>("res://scenes/ui/InventoryPanel.tscn").Instantiate<InventoryPanelController>();
        _shopPanel = GD.Load<PackedScene>("res://scenes/ui/ShopPanel.tscn").Instantiate<ShopPanelController>();
        _storagePanel = GD.Load<PackedScene>("res://scenes/ui/StoragePanel.tscn").Instantiate<StoragePanelController>();
        AddChild(_inventoryPanel);
        AddChild(_shopPanel);
        AddChild(_storagePanel);

        RenderPanels();

        GD.Print($"Loaded {crops.Count} crops and {items.Count} items, {_shopOffers.Count} shop offers, and {_requests.Count} requests.");
        GD.Print($"Day {_clock.Date.Day} of {_clock.Date.Season}, stamina {_stamina.Current}/{_stamina.Maximum}");

        RefreshHud();
        if (ShouldAutosaveAfterBootstrap(loadedExistingSave: false, hasMeaningfulStateChanges: false))
        {
            Autosave();
        }
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

    public static bool ShouldAutosaveAfterBootstrap(bool loadedExistingSave, bool hasMeaningfulStateChanges)
    {
        return loadedExistingSave && hasMeaningfulStateChanges;
    }

    public static bool TryApplyShopOpenSideEffects(
        InventoryState inventory,
        Wallet wallet,
        IReadOnlyList<ShopOffer> offers)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentNullException.ThrowIfNull(offers);
        return false;
    }

    private static IReadOnlyList<T> DeserializeList<T>(string json, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"Catalog '{sourceName}' was empty.");
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Catalog '{sourceName}' was invalid JSON.", exception);
        }
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

    private void WireTownScene(Node townScene)
    {
        var shop = townScene.GetNodeOrNull<ShopInteraction>("Shop");
        if (shop is null)
        {
            GD.PushWarning("Town scene is missing a ShopInteraction node named 'Shop'.");
        }
        else
        {
            shop.ShopRequested += OnShopRequested;
        }

        var storage = townScene.GetNodeOrNull<StorageInteraction>("Storage");
        if (storage is null)
        {
            GD.PushWarning("Town scene is missing a StorageInteraction node named 'Storage'.");
        }
        else
        {
            storage.StorageRequested += OnStorageRequested;
        }

        var requestBoard = townScene.GetNodeOrNull<RequestBoardInteraction>("RequestBoard");
        if (requestBoard is null)
        {
            GD.PushWarning("Town scene is missing a RequestBoardInteraction node named 'RequestBoard'.");
        }
        else
        {
            requestBoard.RequestBoardRequested += OnRequestBoardRequested;
        }
    }

    private void OnDayEndRequested()
    {
        EndDay();
    }

    private void OnShopRequested()
    {
        if (_inventory is null || _wallet is null)
        {
            return;
        }

        _ = TryApplyShopOpenSideEffects(_inventory, _wallet, _shopOffers);
        RenderPanels();
        TogglePanel(_shopPanel);
    }

    private void OnStorageRequested()
    {
        RenderPanels();
        TogglePanel(_inventoryPanel);
        TogglePanel(_storagePanel);
    }

    private void OnRequestBoardRequested()
    {
        if (_inventory is null || _wallet is null)
        {
            return;
        }

        var nextRequest = _requests.FirstOrDefault(request => !_completedRequestIds.Contains(request.Id));
        if (nextRequest is null)
        {
            return;
        }

        if (_requestBoardService.TryComplete(nextRequest, _inventory, _completedRequestIds, out var rewardGold))
        {
            _wallet.Earn(rewardGold);
            RefreshHud();
            Autosave();
        }

        RenderPanels();
    }

    private bool TryPurchaseExpansion(string plotKey, int requiredGold)
    {
        if (_wallet is null)
        {
            return false;
        }

        if (!_expansionService.TryUnlockPlot(_unlockState, plotKey, requiredGold, _wallet.Gold, out var updatedGold))
        {
            return false;
        }

        _wallet = new Wallet(updatedGold);
        RefreshHud();
        Autosave();
        return true;
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
            Autosave();
        }
    }

    private void TogglePanel(Control? panel)
    {
        if (panel is null)
        {
            return;
        }

        panel.Visible = !panel.Visible;
    }

    private void RenderPanels()
    {
        if (_inventory is not null)
        {
            _inventoryPanel?.Render(_inventory);
        }

        _shopPanel?.Render(_shopOffers);

        if (_storage is not null)
        {
            _storagePanel?.Render(_storage);
        }
    }

    private void Autosave()
    {
        if (_clock is null || _wallet is null || _stamina is null || _inventory is null || _storage is null || _farmGrid is null)
        {
            return;
        }

        var snapshot = new SaveGameSnapshot(
            _clock.Date,
            _clock.CurrentMinuteOfDay,
            _wallet.Gold,
            _stamina.Current,
            _inventory.Slots.ToList(),
            _storage.Slots.ToList(),
            _farmGrid.AllPlots.Select(plot => new PlotSnapshot(
                plot.X,
                plot.Y,
                plot.IsTilled,
                plot.IsLocked,
                plot.IsWateredToday,
                plot.IsHarvestReady,
                plot.Crop?.CropId,
                plot.Crop?.DaysGrown ?? 0)).ToList(),
            _unlockState.UnlockedPlotKeys.OrderBy(static key => key).ToList(),
            _completedRequestIds.ToList());

        var saveDir = ProjectSettings.GlobalizePath("user://saves");
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, "slot-1.json"), SaveGameStore.Serialize(snapshot));
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
        _hud.SetGrowth($"Unlocked plots: {_unlockState.UnlockedPlotKeys.Count}");
    }
}
