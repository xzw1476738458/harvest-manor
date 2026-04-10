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
    private const int DefaultInventorySlots = 12;
    private const int DefaultStorageSlots = 24;
    private const int DefaultMaxStackSize = 99;
    private const int DefaultFarmWidth = 6;
    private const int DefaultFarmHeight = 6;
    private const string DemoExpansionPlotKey = "2,0";
    private const int DemoExpansionCost = 120;

    private static readonly string[] DefaultUnlockedPlotKeys = { "0,0", "1,0", "0,1", "1,1" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ContentCatalogLoader _loader = new();
    private readonly RequestBoardService _requestBoardService = new();
    private readonly FarmExpansionService _expansionService = new();
    private readonly ShopService _shopService = new();
    private readonly HashSet<string> _completedRequestIds = new();
    private readonly UnlockState _unlockState = new(new HashSet<string>(DefaultUnlockedPlotKeys));
    private readonly Dictionary<string, CropDefinition> _cropCatalog = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ItemDefinition> _itemCatalog = new(StringComparer.Ordinal);
    private readonly List<FarmPlotNode> _farmPlotNodes = new();

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
    private Label? _farmStatusLabel;
    private Label? _requestStatusLabel;
    private IReadOnlyList<ShopOffer> _shopOffers = Array.Empty<ShopOffer>();
    private IReadOnlyList<RequestDefinition> _requests = Array.Empty<RequestDefinition>();
    private int _selectedShopOfferIndex;
    private PanelMode _activePanelMode = PanelMode.None;
    private string _persistedFarmStatusMessage = string.Empty;
    private string _latestPanelContextFarmStatusMessage = string.Empty;

    public sealed record RuntimeState(
        DayClock Clock,
        StaminaState Stamina,
        Wallet Wallet,
        InventoryState Inventory,
        InventoryState Storage,
        FarmGrid FarmGrid);

    public enum PanelMode
    {
        None,
        Shop,
        Storage
    }

    public readonly record struct PanelVisibility(bool InventoryVisible, bool ShopVisible, bool StorageVisible);

    public override void _Ready()
    {
        var savePath = GetSaveSlotPath();
        var saveFileExists = File.Exists(savePath);
        var cropCatalogJson = Godot.FileAccess.GetFileAsString("res://data/crops/spring.json");
        var itemCatalogJson = Godot.FileAccess.GetFileAsString("res://data/items/items.json");
        var shopCatalogJson = Godot.FileAccess.GetFileAsString("res://data/shops/general-store.json");
        var requestCatalogJson = Godot.FileAccess.GetFileAsString("res://data/requests/request-board.json");

        var crops = _loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = _loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        _shopOffers = DeserializeList<ShopOffer>(shopCatalogJson, "res://data/shops/general-store.json");
        _requests = DeserializeList<RequestDefinition>(requestCatalogJson, "res://data/requests/request-board.json");

        _cropCatalog.Clear();
        foreach (var crop in crops)
        {
            _cropCatalog[crop.Id] = crop;
        }

        _itemCatalog.Clear();
        foreach (var item in items)
        {
            _itemCatalog[item.Id] = item;
        }

        _growth = new CropGrowthService(_cropCatalog);

        var loadedExistingSave = TryLoadSnapshotFromPath(savePath, out var snapshot);
        var state = loadedExistingSave && snapshot is not null
            ? CreateRuntimeStateFromSnapshot(snapshot, _unlockState, _completedRequestIds)
            : CreateDefaultRuntimeState();

        _clock = state.Clock;
        _stamina = state.Stamina;
        _wallet = state.Wallet;
        _inventory = state.Inventory;
        _storage = state.Storage;
        _farmGrid = state.FarmGrid;

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
        WireUiPanels();
        ApplyPanelVisibility();

        RenderFarmPlots();
        RenderPanels();

        GD.Print($"Loaded {crops.Count} crops and {items.Count} items, {_shopOffers.Count} shop offers, and {_requests.Count} requests.");
        GD.Print($"Day {_clock.Date.Day} of {_clock.Date.Season}, stamina {_stamina.Current}/{_stamina.Maximum}");

        RefreshHud();
        RefreshRequestBoardStatus();
        SetFarmStatus(BuildStartupFarmStatusMessage(saveFileExists, loadedExistingSave, _farmGrid, _requests, _completedRequestIds, _inventory, _itemCatalog));

        if (ShouldAutosaveAfterBootstrap(saveFileExists, loadedExistingSave, hasMeaningfulStateChanges: false))
        {
            Autosave();
        }
    }
}
