using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using HarvestManor.Core;
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
    private const int DefaultStartingGold = 200;
    private const int DefaultMaximumStamina = 100;
    private const int DayStartMinute = 6 * 60;
    private const int DayEndMinute = 26 * 60;

    private static readonly ExpansionTierService DefaultExpansionTiers = ExpansionTierService.CreateDefault();
    private static readonly IReadOnlyList<string> DefaultUnlockedPlotKeys = DefaultExpansionTiers.DefaultUnlockedPlotKeys.ToList();

    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.ReadOptions;

    private readonly ContentCatalogLoader _loader = new();
    private readonly RequestBoardService _requestBoardService = new();
    private readonly FarmExpansionService _expansionService = new();
    private readonly ExpansionTierService _expansionTiers = ExpansionTierService.CreateDefault();
    private readonly ShopService _shopService = new();
    private readonly HashSet<string> _completedRequestIds = new();
    private readonly UnlockState _unlockState = new(new HashSet<string>(ExpansionTierService.CreateDefault().DefaultUnlockedPlotKeys));
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
    private Control? _farmStatusPanel;
    private Control? _requestStatusPanel;
    private Godot.Timer? _farmStatusTimer;
    private Godot.Timer? _requestStatusTimer;
    private double _realTimeAccumulator;
    private const float MinutesPerRealSecond = 6f;
    private const double StatusVisibleSeconds = 5.0;
    private Node2D? _activeScene;
    private string _activeSceneType = string.Empty;
    private CharacterBody2D? _player;
    private const string FarmSceneType = "farm";
    private const string TownSceneType = "town";
    private const string CottageSceneType = "cottage";
    private const string ShopInteriorSceneType = "shop_interior";
    private const string BarnInteriorSceneType = "barn_interior";
    private static readonly Vector2 FarmDefaultSpawn = new(640, 470);
    private static readonly Vector2 FarmFromTownSpawn = new(1180, 470);
    private static readonly Vector2 FarmFromCottageSpawn = new(1080, 480);
    private static readonly Vector2 TownFromFarmSpawn = new(110, 470);
    private static readonly Vector2 TownFromShopSpawn = new(1000, 470);
    private static readonly Vector2 TownFromBarnSpawn = new(700, 510);
    private static readonly Vector2 CottageEntrySpawn = new(640, 540);
    private static readonly Vector2 ShopInteriorEntrySpawn = new(640, 540);
    private static readonly Vector2 BarnInteriorEntrySpawn = new(640, 540);
    private IReadOnlyList<ShopOffer> _allShopOffers = Array.Empty<ShopOffer>();
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

    private static readonly string[] CropCatalogPaths =
    {
        "res://data/crops/spring.json",
        "res://data/crops/summer.json",
        "res://data/crops/autumn.json",
        "res://data/crops/winter.json"
    };

    public override void _Ready()
    {
        var savePath = GetSaveSlotPath();
        var saveFileExists = File.Exists(savePath);
        var allCrops = new List<CropDefinition>();
        IReadOnlyList<ItemDefinition> items;
        try
        {
            foreach (var cropPath in CropCatalogPaths)
            {
                var json = Godot.FileAccess.GetFileAsString(cropPath);
                allCrops.AddRange(_loader.ParseCropCatalogJson(json, cropPath));
            }

            var itemCatalogJson = Godot.FileAccess.GetFileAsString("res://data/items/items.json");
            var shopCatalogJson = Godot.FileAccess.GetFileAsString("res://data/shops/general-store.json");
            var requestCatalogJson = Godot.FileAccess.GetFileAsString("res://data/requests/request-board.json");

            items = _loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
            _allShopOffers = DeserializeList<ShopOffer>(shopCatalogJson, "res://data/shops/general-store.json");
            _requests = DeserializeList<RequestDefinition>(requestCatalogJson, "res://data/requests/request-board.json");

            foreach (var offer in _allShopOffers) { offer.Validate(); }
            foreach (var request in _requests) { request.Validate(); }
        }
        catch (Exception ex)
        {
            GD.PushError($"Failed to load game data: {ex.Message}");
            return;
        }

        _cropCatalog.Clear();
        foreach (var crop in allCrops)
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

        _player = GD.Load<PackedScene>("res://scenes/world/Player.tscn").Instantiate<CharacterBody2D>();
        AddChild(_player);
        _player.Position = FarmDefaultSpawn;

        LoadScene(FarmSceneType, FarmDefaultSpawn);

        _hud = GD.Load<PackedScene>("res://scenes/ui/Hud.tscn").Instantiate<HudController>();
        AddChild(_hud);

        _farmStatusTimer = new Godot.Timer { OneShot = true, WaitTime = StatusVisibleSeconds };
        AddChild(_farmStatusTimer);
        _farmStatusTimer.Timeout += HideFarmStatusPanel;

        _requestStatusTimer = new Godot.Timer { OneShot = true, WaitTime = StatusVisibleSeconds };
        AddChild(_requestStatusTimer);
        _requestStatusTimer.Timeout += HideRequestStatusPanel;

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

        _shopOffers = BuildSeasonShopOffers(_allShopOffers, _cropCatalog, _clock.Date.Season);
        _selectedShopOfferIndex = 0;

        GD.Print($"Loaded {allCrops.Count} crops and {items.Count} items, {_shopOffers.Count} shop offers, and {_requests.Count} requests.");
        GD.Print($"Day {_clock.Date.Day} of {_clock.Date.Season}, stamina {_stamina.Current}/{_stamina.Maximum}");

        RefreshHud();
        RefreshRequestBoardStatus();
        SetFarmStatus(StatusMessageBuilder.BuildStartupFarmStatusMessage(saveFileExists, loadedExistingSave, _farmGrid, _requests, _completedRequestIds, _inventory, _itemCatalog));
        UpdateTimeOfDayVisuals();

        if (ShouldAutosaveAfterBootstrap(saveFileExists, loadedExistingSave, hasMeaningfulStateChanges: false))
        {
            Autosave();
        }
    }
}
