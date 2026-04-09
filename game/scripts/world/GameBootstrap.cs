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

    public static bool ShouldAutosaveAfterBootstrap(bool saveFileExists, bool loadedExistingSave, bool hasMeaningfulStateChanges)
    {
        return saveFileExists && !loadedExistingSave
            || loadedExistingSave && hasMeaningfulStateChanges;
    }

    public static string BuildStartupFarmStatusMessage(bool saveFileExists, bool loadedExistingSave)
    {
        return BuildStartupFarmStatusMessage(saveFileExists, loadedExistingSave, farmGrid: null);
    }

    public static string BuildStartupFarmStatusMessage(bool saveFileExists, bool loadedExistingSave, FarmGrid? farmGrid)
    {
        return BuildStartupFarmStatusMessage(
            saveFileExists,
            loadedExistingSave,
            farmGrid,
            requests: null,
            completedRequestIds: null,
            inventory: null,
            itemCatalog: null);
    }

    public static string BuildStartupFarmStatusMessage(
        bool saveFileExists,
        bool loadedExistingSave,
        FarmGrid? farmGrid,
        IReadOnlyList<RequestDefinition>? requests,
        ISet<string>? completedRequestIds,
        InventoryState? inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        if (loadedExistingSave)
        {
            if (farmGrid is not null)
            {
                var harvestReadyCount = farmGrid.AllPlots.Count(static plot => plot.IsHarvestReady);
                if (harvestReadyCount > 0)
                {
                    return $"Save loaded. {harvestReadyCount} {(harvestReadyCount == 1 ? "crop is" : "crops are")} ready to harvest.";
                }

                var waterNeededCount = farmGrid.AllPlots.Count(static plot => plot.Crop is not null && !plot.IsHarvestReady && !plot.IsWateredToday);
                if (waterNeededCount > 0)
                {
                    return $"Save loaded. {waterNeededCount} planted {(waterNeededCount == 1 ? "crop still needs" : "crops still need")} water.";
                }
            }

            if (requests is not null && completedRequestIds is not null && inventory is not null)
            {
                var requestStatus = BuildRequestBoardStatusText(requests, completedRequestIds, inventory, itemCatalog);
                if (!string.IsNullOrWhiteSpace(requestStatus))
                {
                    return $"Save loaded. {requestStatus}";
                }
            }

            return "Save loaded. Click a plot to till, plant, water, or harvest.";
        }

        return saveFileExists
            ? "Previous save could not be read. Started a fresh day instead."
            : "Fresh start. Click a plot to till, plant, water, or harvest.";
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

    public static string GetLockedPlotHint(int x, int y)
    {
        return BuildPlotKey(x, y) == DemoExpansionPlotKey
            ? $"Click: unlock ({DemoExpansionCost}g)"
            : "Locked";
    }

    public static bool TryHandleLockedPlotInteraction(
        FarmExpansionService expansionService,
        UnlockState unlockState,
        int currentGold,
        int x,
        int y,
        out int updatedGold,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(expansionService);
        ArgumentNullException.ThrowIfNull(unlockState);

        var plotKey = BuildPlotKey(x, y);
        if (plotKey != DemoExpansionPlotKey)
        {
            updatedGold = currentGold;
            message = "Plot is locked.";
            return false;
        }

        if (expansionService.TryUnlockPlot(unlockState, plotKey, DemoExpansionCost, currentGold, out updatedGold))
        {
            message = $"Unlocked a new plot for {DemoExpansionCost}g.";
            return true;
        }

        message = currentGold < DemoExpansionCost
            ? $"Need {DemoExpansionCost}g to unlock this plot."
            : "Plot is locked.";
        return false;
    }

    public static PanelVisibility ResolvePanelVisibility(PanelMode mode)
    {
        return mode switch
        {
            PanelMode.Shop => new PanelVisibility(false, true, false),
            PanelMode.Storage => new PanelVisibility(true, false, true),
            _ => new PanelVisibility(false, false, false)
        };
    }

    public static bool BlocksWorldInteractions(PanelMode mode)
    {
        return mode != PanelMode.None;
    }

    public static PanelMode ResolvePanelModeAfterUnhandledKey(PanelMode currentMode, Key keycode)
    {
        return keycode == Key.Escape && currentMode != PanelMode.None
            ? PanelMode.None
            : currentMode;
    }

    public static bool CanHandlePanelInteractionRequest(PanelMode currentMode, PanelMode requestedMode)
    {
        return requestedMode != PanelMode.None
            && (currentMode == PanelMode.None || currentMode == requestedMode);
    }

    public static PanelMode ResolvePanelModeAfterInteractionRequest(PanelMode currentMode, PanelMode requestedMode)
    {
        return currentMode == requestedMode
            ? PanelMode.None
            : requestedMode;
    }

    public static bool CanTriggerDemoExpansionShortcut(PanelMode currentMode, Key keycode)
    {
        return keycode == Key.F7 && !BlocksWorldInteractions(currentMode);
    }

    public static string? BuildBlockedWorldInteractionMessage(PanelMode mode, PanelMode requestedMode = PanelMode.None)
    {
        if (mode != PanelMode.None && mode == requestedMode)
        {
            return mode switch
            {
                PanelMode.Shop => "Shop open. Click again or press Esc to close.",
                PanelMode.Storage => "Storage open. Click again or press Esc to close.",
                _ => null
            };
        }

        if (mode == PanelMode.Shop && requestedMode == PanelMode.Storage)
        {
            return "Close the shop panel before opening storage.";
        }

        if (mode == PanelMode.Storage && requestedMode == PanelMode.Shop)
        {
            return "Close the storage panel before opening shop.";
        }

        return mode switch
        {
            PanelMode.Shop => "Close the shop panel before interacting with the world.",
            PanelMode.Storage => "Close the storage panel before interacting with the world.",
            _ => null
        };
    }

    public static string? BuildPanelModeStatusMessage(PanelMode previousMode, PanelMode nextMode)
    {
        if (previousMode == nextMode)
        {
            return null;
        }

        return nextMode switch
        {
            PanelMode.Shop => "Shop open. Use Buy/Sell or press Esc to close.",
            PanelMode.Storage => "Storage open. Move items or press Esc to close.",
            _ => null
        };
    }

    public static string BuildPanelCloseStatusMessage(PanelMode previousMode, string? latestPanelContextMessage)
    {
        if (previousMode != PanelMode.None && !string.IsNullOrWhiteSpace(latestPanelContextMessage))
        {
            return latestPanelContextMessage;
        }

        return "Panels closed. Interact with the world again.";
    }

    public static string BuildFarmPlotHoverStatusMessage(
        PlotState plot,
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState? inventory = null,
        int? currentGold = null)
    {
        ArgumentNullException.ThrowIfNull(plot);
        ArgumentNullException.ThrowIfNull(crops);

        if (plot.IsLocked)
        {
            if (BuildPlotKey(plot.X, plot.Y) == DemoExpansionPlotKey && currentGold is not null && currentGold < DemoExpansionCost)
            {
                return $"Hover plot: need {DemoExpansionCost}g to unlock.";
            }

            return BuildPlotKey(plot.X, plot.Y) == DemoExpansionPlotKey
                ? $"Hover plot: unlock for {DemoExpansionCost}g."
                : "Hover plot: locked.";
        }

        if (!plot.IsTilled)
        {
            return "Hover plot: click to till.";
        }

        if (plot.Crop is null)
        {
            if (inventory is null)
            {
                return "Hover plot: click to plant.";
            }

            var cropToPlant = FindAutoPlantCrop(crops, inventory);
            if (cropToPlant is null)
            {
                return "Hover plot: no seeds available.";
            }

            return $"Hover plot: click to plant {cropToPlant.DisplayName}.";
        }

        var cropName = crops.TryGetValue(plot.Crop.CropId, out var crop)
            ? crop.DisplayName
            : plot.Crop.CropId;

        if (plot.IsHarvestReady && crop is not null && inventory is not null && !inventory.CanAdd(crop.HarvestItemId, 1))
        {
            return $"Hover {cropName}: inventory full.";
        }

        if (plot.IsHarvestReady)
        {
            return $"Hover {cropName}: ready to harvest.";
        }

        if (plot.IsWateredToday)
        {
            return $"Hover {cropName}: watered today.";
        }

        return $"Hover {cropName}: click to water.";
    }

    public static string BuildInteractionHoverStatusMessage(string interactionName, string actionDescription)
    {
        if (string.IsNullOrWhiteSpace(interactionName))
        {
            throw new ArgumentException("Interaction name cannot be blank.", nameof(interactionName));
        }

        if (string.IsNullOrWhiteSpace(actionDescription))
        {
            throw new ArgumentException("Action description cannot be blank.", nameof(actionDescription));
        }

        return $"Hover {interactionName}: {actionDescription}.";
    }

    public static string BuildRequestBoardHoverStatusMessage(
        IReadOnlyList<RequestDefinition> requests,
        ISet<string> completedRequestIds,
        InventoryState inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(inventory);

        var nextRequest = requests.FirstOrDefault(request => !completedRequestIds.Contains(request.Id));
        if (nextRequest is null)
        {
            return "Hover request board: all requests completed.";
        }

        var currentQuantity = inventory.GetQuantity(nextRequest.RequiredItemId);
        var itemDisplayName = ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog);
        if (currentQuantity >= nextRequest.RequiredQuantity)
        {
            return $"Hover request board: turn in {nextRequest.RequiredQuantity} {itemDisplayName} for {nextRequest.RewardGold}g.";
        }

        var remainingQuantity = nextRequest.RequiredQuantity - currentQuantity;
        return $"Hover request board: need {remainingQuantity} more {itemDisplayName}.";
    }

    public static string BuildShopBrowseStatusMessage(
        IReadOnlyList<ShopOffer> offers,
        int selectedOfferIndex,
        InventoryState inventory,
        Wallet wallet,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(offers);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(wallet);

        if (offers.Count == 0)
        {
            return "Shop open. No offers available.";
        }

        var clampedIndex = Math.Clamp(selectedOfferIndex, 0, offers.Count - 1);
        var offer = offers[clampedIndex];
        var state = ShopPanelController.EvaluateOfferState(offer, inventory, wallet);
        return $"Shop selection: {ItemDisplayNameFormatter.Resolve(offer.ItemId, itemCatalog)}. {state.StatusText}";
    }

    public static string BuildStorageBrowseStatusMessage(
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(storage);

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);
        var storeDisplayName = state.StoreCandidateItemId is null
            ? null
            : ItemDisplayNameFormatter.Resolve(state.StoreCandidateItemId, itemCatalog);
        var withdrawDisplayName = state.WithdrawCandidateItemId is null
            ? null
            : ItemDisplayNameFormatter.Resolve(state.WithdrawCandidateItemId, itemCatalog);

        if (state.StoreCandidateItemId is null && state.WithdrawCandidateItemId is null)
        {
            return "Storage open. Nothing to move.";
        }

        if (state.CanStore && state.CanWithdraw &&
            state.StoreCandidateItemId is not null &&
            state.WithdrawCandidateItemId is not null)
        {
            return $"Storage selection: store {storeDisplayName} or take {withdrawDisplayName}.";
        }

        if (state.CanStore && state.StoreCandidateItemId is not null)
        {
            return state.WithdrawCandidateItemId is not null
                ? $"Storage selection: store {storeDisplayName}. Cannot take {withdrawDisplayName}: inventory is full."
                : $"Storage selection: store {storeDisplayName}.";
        }

        if (state.CanWithdraw && state.WithdrawCandidateItemId is not null)
        {
            return state.StoreCandidateItemId is not null
                ? $"Storage selection: take {withdrawDisplayName}. Cannot store {storeDisplayName}: storage is full."
                : $"Storage selection: take {withdrawDisplayName}.";
        }

        if (state.StoreCandidateItemId is not null && state.WithdrawCandidateItemId is not null)
        {
            return $"Storage blocked: cannot store {storeDisplayName} (storage full) or take {withdrawDisplayName} (inventory full).";
        }

        if (state.StoreCandidateItemId is not null)
        {
            return $"Storage blocked: cannot store {storeDisplayName} (storage full).";
        }

        if (state.WithdrawCandidateItemId is not null)
        {
            return $"Storage blocked: cannot take {withdrawDisplayName} (inventory full).";
        }

        return "Storage open. Nothing to move.";
    }

    public static string BuildShopActionStatusMessage(
        string actionMessage,
        IReadOnlyList<ShopOffer> offers,
        int selectedOfferIndex,
        InventoryState inventory,
        Wallet wallet,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionMessage);
        return $"{actionMessage} {BuildShopBrowseStatusMessage(offers, selectedOfferIndex, inventory, wallet, itemCatalog)}";
    }

    public static string BuildStorageActionStatusMessage(
        string actionMessage,
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionMessage);
        return $"{actionMessage} {BuildStorageBrowseStatusMessage(inventory, storage, itemCatalog)}";
    }

    public static string BuildShopPurchaseStatusMessage(
        ShopOffer offer,
        InventoryState inventory,
        Wallet wallet,
        bool changed,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(wallet);

        var itemDisplayName = ItemDisplayNameFormatter.Resolve(offer.ItemId, itemCatalog);

        if (changed)
        {
            return $"Bought 1 {itemDisplayName} for {offer.BuyPrice}g.";
        }

        if (!inventory.CanAdd(offer.ItemId, 1))
        {
            return $"Cannot buy {itemDisplayName}: inventory full.";
        }

        var missingGold = Math.Max(0, offer.BuyPrice - wallet.Gold);
        if (missingGold > 0)
        {
            return $"Need {missingGold}g more to buy 1 {itemDisplayName}.";
        }

        return $"Cannot buy {itemDisplayName}.";
    }

    public static string BuildShopSellStatusMessage(
        ShopOffer offer,
        InventoryState inventory,
        bool changed,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(inventory);

        var itemDisplayName = ItemDisplayNameFormatter.Resolve(offer.ItemId, itemCatalog);

        if (changed)
        {
            return $"Sold 1 {itemDisplayName} for {offer.SellPrice}g.";
        }

        return inventory.GetQuantity(offer.ItemId) > 0
            ? $"Cannot sell {itemDisplayName}."
            : $"Cannot sell {itemDisplayName}: none available.";
    }

    public static string BuildStorageTransferStatusMessage(
        string itemId,
        bool changed,
        bool intoStorage,
        InventoryState source,
        InventoryState destination,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var itemDisplayName = ItemDisplayNameFormatter.Resolve(itemId, itemCatalog);

        if (changed)
        {
            return intoStorage
                ? $"Stored 1 {itemDisplayName}."
                : $"Took 1 {itemDisplayName} from storage.";
        }

        if (source.GetQuantity(itemId) <= 0)
        {
            return intoStorage
                ? $"Cannot store {itemDisplayName}: none available."
                : $"Cannot take {itemDisplayName}: none available.";
        }

        if (!destination.CanAdd(itemId, 1))
        {
            return intoStorage
                ? $"Cannot store {itemDisplayName}: storage is full."
                : $"Cannot take {itemDisplayName}: inventory is full.";
        }

        return intoStorage
            ? $"Cannot store {itemDisplayName}."
            : $"Cannot take {itemDisplayName}.";
    }

    public static string BuildRequestBoardStatusText(
        IReadOnlyList<RequestDefinition> requests,
        ISet<string> completedRequestIds,
        InventoryState inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(inventory);

        var nextRequest = requests.FirstOrDefault(request => !completedRequestIds.Contains(request.Id));
        if (nextRequest is null)
        {
            return "All requests completed.";
        }

        var currentQuantity = inventory.GetQuantity(nextRequest.RequiredItemId);
        var itemDisplayName = ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog);
        if (currentQuantity >= nextRequest.RequiredQuantity)
        {
            return $"Request ready: {itemDisplayName} {currentQuantity}/{nextRequest.RequiredQuantity}. Click board to turn in.";
        }

        var remainingQuantity = nextRequest.RequiredQuantity - currentQuantity;
        return $"Active request: {itemDisplayName} {currentQuantity}/{nextRequest.RequiredQuantity}. Need {remainingQuantity} more.";
    }

    public static string BuildPlotKey(int x, int y)
    {
        return $"{x},{y}";
    }

    public static bool IsPlotUnlocked(UnlockState unlockState, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(unlockState);
        return unlockState.UnlockedPlotKeys.Contains(BuildPlotKey(x, y));
    }

    public static void SyncFarmGridLocksFromUnlockState(FarmGrid farmGrid, UnlockState unlockState)
    {
        ArgumentNullException.ThrowIfNull(farmGrid);
        ArgumentNullException.ThrowIfNull(unlockState);

        foreach (var plot in farmGrid.AllPlots.ToList())
        {
            var isUnlocked = IsPlotUnlocked(unlockState, plot.X, plot.Y);
            farmGrid.SetPlot(plot with { IsLocked = !isUnlocked });
        }
    }

    public static List<PlotSnapshot> CreatePlotSnapshots(FarmGrid farmGrid, UnlockState unlockState)
    {
        ArgumentNullException.ThrowIfNull(farmGrid);
        ArgumentNullException.ThrowIfNull(unlockState);

        return farmGrid.AllPlots.Select(plot => new PlotSnapshot(
            plot.X,
            plot.Y,
            plot.IsTilled,
            !IsPlotUnlocked(unlockState, plot.X, plot.Y),
            plot.IsWateredToday,
            plot.IsHarvestReady,
            plot.Crop?.CropId,
            plot.Crop?.DaysGrown ?? 0)).ToList();
    }

    public static bool TryLoadSnapshotFromPath(string savePath, out SaveGameSnapshot? snapshot)
    {
        if (string.IsNullOrWhiteSpace(savePath))
        {
            throw new ArgumentException("Save path cannot be blank.", nameof(savePath));
        }

        snapshot = null;

        if (!File.Exists(savePath))
        {
            return false;
        }

        try
        {
            snapshot = SaveGameStore.Deserialize(File.ReadAllText(savePath));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return false;
        }
    }

    public static RuntimeState CreateRuntimeStateFromSnapshot(
        SaveGameSnapshot snapshot,
        UnlockState unlockState,
        ISet<string> completedRequestIds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(unlockState);
        ArgumentNullException.ThrowIfNull(completedRequestIds);

        unlockState.UnlockedPlotKeys.Clear();
        var restoredPlotKeys = snapshot.UnlockedPlotKeys.Count > 0
            ? snapshot.UnlockedPlotKeys
            : DefaultUnlockedPlotKeys.ToList();
        foreach (var plotKey in restoredPlotKeys.Distinct(StringComparer.Ordinal))
        {
            unlockState.UnlockedPlotKeys.Add(plotKey);
        }

        completedRequestIds.Clear();
        foreach (var requestId in snapshot.CompletedRequests.Distinct(StringComparer.Ordinal))
        {
            completedRequestIds.Add(requestId);
        }

        var inventory = new InventoryState(DefaultInventorySlots, DefaultMaxStackSize);
        inventory.RestoreSnapshot(snapshot.Inventory);

        var storage = new InventoryState(DefaultStorageSlots, DefaultMaxStackSize);
        storage.RestoreSnapshot(snapshot.Storage);

        var farmGrid = new FarmGrid(DefaultFarmWidth, DefaultFarmHeight);
        foreach (var plotSnapshot in snapshot.Plots)
        {
            farmGrid.SetPlot(CreatePlotState(plotSnapshot));
        }

        SyncFarmGridLocksFromUnlockState(farmGrid, unlockState);

        var clock = new DayClock(snapshot.Date, 6 * 60, 26 * 60);
        if (snapshot.MinuteOfDay > clock.CurrentMinuteOfDay)
        {
            clock.AdvanceMinutes(snapshot.MinuteOfDay - clock.CurrentMinuteOfDay);
        }

        return new RuntimeState(
            clock,
            new StaminaState(maximum: 100, current: snapshot.Stamina),
            new Wallet(snapshot.Gold),
            inventory,
            storage,
            farmGrid);
    }

    public static bool TryHandleFarmPlotInteraction(
        FarmGrid farmGrid,
        InventoryState inventory,
        IReadOnlyDictionary<string, CropDefinition> crops,
        int x,
        int y,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(farmGrid);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(crops);

        var plot = farmGrid.GetPlot(x, y);
        if (plot.IsLocked)
        {
            message = "Plot is locked.";
            return false;
        }

        if (!plot.IsTilled)
        {
            farmGrid.SetPlot(plot.Till());
            message = "Plot tilled.";
            return true;
        }

        if (plot.Crop is null)
        {
            var cropToPlant = FindAutoPlantCrop(crops, inventory);

            if (cropToPlant is null)
            {
                message = "No seeds available.";
                return false;
            }

            if (!inventory.TryRemove(cropToPlant.SeedItemId, 1))
            {
                message = "No seeds available.";
                return false;
            }

            farmGrid.SetPlot(plot.Plant(cropToPlant.Id));
            message = $"Planted {cropToPlant.DisplayName}.";
            return true;
        }

        if (!crops.TryGetValue(plot.Crop.CropId, out var cropDefinition))
        {
            throw new InvalidOperationException($"Unknown crop id '{plot.Crop.CropId}' in plot state.");
        }

        if (plot.IsHarvestReady)
        {
            if (!inventory.TryAdd(cropDefinition.HarvestItemId, 1))
            {
                message = "Inventory full.";
                return false;
            }

            farmGrid.SetPlot(plot with
            {
                Crop = null,
                IsWateredToday = false,
                IsHarvestReady = false
            });

            message = $"Harvested {cropDefinition.DisplayName}.";
            return true;
        }

        if (!plot.IsWateredToday)
        {
            farmGrid.SetPlot(plot.Water());
            message = "Watered plot.";
            return true;
        }

        message = "Crop already watered today.";
        return false;
    }

    private static CropDefinition? FindAutoPlantCrop(
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState inventory)
    {
        ArgumentNullException.ThrowIfNull(crops);
        ArgumentNullException.ThrowIfNull(inventory);

        return crops.Values
            .OrderBy(static crop => crop.DisplayName, StringComparer.Ordinal)
            .FirstOrDefault(crop => inventory.GetQuantity(crop.SeedItemId) > 0);
    }

    public static bool TryTransferItem(InventoryState source, InventoryState destination, string itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        if (source.GetQuantity(itemId) < quantity)
        {
            return false;
        }

        var sourceSnapshot = source.CreateSnapshot();
        var destinationSnapshot = destination.CreateSnapshot();

        if (!source.TryRemove(itemId, quantity))
        {
            return false;
        }

        if (destination.TryAdd(itemId, quantity))
        {
            return true;
        }

        source.RestoreSnapshot(sourceSnapshot);
        destination.RestoreSnapshot(destinationSnapshot);
        return false;
    }

    public static bool TryCompleteNextRequest(
        IReadOnlyList<RequestDefinition> requests,
        RequestBoardService requestBoardService,
        InventoryState inventory,
        ISet<string> completedRequestIds,
        Wallet wallet,
        out string message)
    {
        return TryCompleteNextRequest(
            requests,
            requestBoardService,
            inventory,
            completedRequestIds,
            wallet,
            itemCatalog: null,
            out message);
    }

    public static bool TryCompleteNextRequest(
        IReadOnlyList<RequestDefinition> requests,
        RequestBoardService requestBoardService,
        InventoryState inventory,
        ISet<string> completedRequestIds,
        Wallet wallet,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(requestBoardService);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(wallet);

        var nextRequest = requests.FirstOrDefault(request => !completedRequestIds.Contains(request.Id));
        if (nextRequest is null)
        {
            message = "All requests completed.";
            return false;
        }

        if (!requestBoardService.TryComplete(nextRequest, inventory, completedRequestIds, out var rewardGold))
        {
            var currentQuantity = inventory.GetQuantity(nextRequest.RequiredItemId);
            var remainingQuantity = Math.Max(0, nextRequest.RequiredQuantity - currentQuantity);
            var itemDisplayName = ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog);
            message = $"Need {remainingQuantity} more {itemDisplayName}.";
            return false;
        }

        wallet.Earn(rewardGold);
        message = itemCatalog is null
            ? $"Completed request {nextRequest.Id} for {rewardGold}g."
            : $"Completed request: delivered {nextRequest.RequiredQuantity} {ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog)} for {rewardGold}g.";
        return true;
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

    private static PlotState CreatePlotState(PlotSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new PlotState(
            snapshot.X,
            snapshot.Y,
            snapshot.IsTilled,
            snapshot.IsLocked,
            snapshot.IsWateredToday,
            snapshot.IsHarvestReady,
            snapshot.CropId is null ? null : new CropInstance(snapshot.CropId, snapshot.DaysGrown));
    }

    private void WireFarmScene(Node farmScene)
    {
        _farmPlotNodes.Clear();
        _farmPlotNodes.AddRange(farmScene.GetChildren().OfType<FarmPlotNode>());
        if (_farmPlotNodes.Count == 0)
        {
            GD.PushWarning("Farm scene is missing FarmPlotNode children.");
        }
        else
        {
            foreach (var plotNode in _farmPlotNodes)
            {
                plotNode.PlotInteracted += OnFarmPlotInteracted;
                plotNode.MouseEntered += () => OnFarmPlotHovered(plotNode.GridX, plotNode.GridY);
                plotNode.MouseExited += OnWorldInteractionHoverEnded;
            }
        }

        _farmStatusLabel = farmScene.GetNodeOrNull<Label>("FarmStatusLabel");

        var bed = farmScene.GetNodeOrNull<BedInteraction>("Bed");
        if (bed is null)
        {
            GD.PushWarning("Farm scene is missing a BedInteraction node named 'Bed'.");
            return;
        }

        bed.DayEndRequested += OnDayEndRequested;
        bed.MouseEntered += () => OnWorldInteractionHovered("bed", "click to end day");
        bed.MouseExited += OnWorldInteractionHoverEnded;
    }

    private void WireTownScene(Node townScene)
    {
        _requestStatusLabel = townScene.GetNodeOrNull<Label>("RequestStatusLabel");

        var shop = townScene.GetNodeOrNull<ShopInteraction>("Shop");
        if (shop is null)
        {
            GD.PushWarning("Town scene is missing a ShopInteraction node named 'Shop'.");
        }
        else
        {
            shop.ShopRequested += OnShopRequested;
            shop.MouseEntered += () => OnWorldInteractionHovered("shop", "buy or sell items", PanelMode.Shop);
            shop.MouseExited += OnWorldInteractionHoverEnded;
        }

        var storage = townScene.GetNodeOrNull<StorageInteraction>("Storage");
        if (storage is null)
        {
            GD.PushWarning("Town scene is missing a StorageInteraction node named 'Storage'.");
        }
        else
        {
            storage.StorageRequested += OnStorageRequested;
            storage.MouseEntered += () => OnWorldInteractionHovered("storage", "move items", PanelMode.Storage);
            storage.MouseExited += OnWorldInteractionHoverEnded;
        }

        var requestBoard = townScene.GetNodeOrNull<RequestBoardInteraction>("RequestBoard");
        if (requestBoard is null)
        {
            GD.PushWarning("Town scene is missing a RequestBoardInteraction node named 'RequestBoard'.");
        }
        else
        {
            requestBoard.RequestBoardRequested += OnRequestBoardRequested;
            requestBoard.MouseEntered += OnRequestBoardHovered;
            requestBoard.MouseExited += OnWorldInteractionHoverEnded;
        }
    }

    private void WireUiPanels()
    {
        if (_shopPanel is not null)
        {
            _shopPanel.BuyRequested += OnShopBuyRequested;
            _shopPanel.SellRequested += OnShopSellRequested;
            _shopPanel.NextOfferRequested += OnShopNextOfferRequested;
            _shopPanel.PreviousOfferRequested += OnShopPreviousOfferRequested;
            _shopPanel.CloseRequested += OnShopCloseRequested;
        }

        if (_storagePanel is not null)
        {
            _storagePanel.StoreRequested += OnStorageStoreRequested;
            _storagePanel.WithdrawRequested += OnStorageWithdrawRequested;
            _storagePanel.CloseRequested += OnStorageCloseRequested;
        }
    }

    private void OnDayEndRequested()
    {
        EndDay();
    }

    private void OnFarmPlotHovered(int gridX, int gridY)
    {
        if (_farmGrid is null)
        {
            return;
        }

        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? BuildBlockedWorldInteractionMessage(_activePanelMode)
                : BuildFarmPlotHoverStatusMessage(_farmGrid.GetPlot(gridX, gridY), _cropCatalog, _inventory, _wallet?.Gold));
    }

    private void OnWorldInteractionHovered(string interactionName, string actionDescription, PanelMode requestedMode = PanelMode.None)
    {
        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? BuildBlockedWorldInteractionMessage(_activePanelMode, requestedMode)
                : BuildInteractionHoverStatusMessage(interactionName, actionDescription));
    }

    private void OnRequestBoardHovered()
    {
        PreviewFarmStatus(
            BlocksWorldInteractions(_activePanelMode)
                ? BuildBlockedWorldInteractionMessage(_activePanelMode)
                : _inventory is null
                    ? BuildInteractionHoverStatusMessage("request board", "turn in crops")
                    : BuildRequestBoardHoverStatusMessage(_requests, _completedRequestIds, _inventory, _itemCatalog));
    }

    private void OnWorldInteractionHoverEnded()
    {
        RestoreFarmStatus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        var nextPanelMode = ResolvePanelModeAfterUnhandledKey(_activePanelMode, keyEvent.Keycode);
        if (nextPanelMode != _activePanelMode)
        {
            SetActivePanelMode(nextPanelMode);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (CanTriggerDemoExpansionShortcut(_activePanelMode, keyEvent.Keycode))
        {
            _ = TryPurchaseExpansion(DemoExpansionPlotKey, requiredGold: DemoExpansionCost);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.F7 && TryNotifyBlockedWorldInteraction())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnFarmPlotInteracted(int gridX, int gridY)
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_farmGrid is null || _inventory is null || _wallet is null || _cropCatalog.Count == 0)
        {
            return;
        }

        var plot = _farmGrid.GetPlot(gridX, gridY);
        var updatedGold = _wallet.Gold;
        string message;
        bool changed;

        if (plot.IsLocked)
        {
            changed = TryHandleLockedPlotInteraction(_expansionService, _unlockState, _wallet.Gold, gridX, gridY, out updatedGold, out message);
        }
        else
        {
            changed = TryHandleFarmPlotInteraction(_farmGrid, _inventory, _cropCatalog, gridX, gridY, out message);
        }

        if (plot.IsLocked && changed)
        {
            _wallet = new Wallet(updatedGold);
            SyncFarmGridLocksFromUnlockState(_farmGrid, _unlockState);
            RefreshHud();
        }

        SetFarmStatus(message);
        RenderFarmPlots();
        RenderPanels();
        RefreshRequestBoardStatus();

        if (changed)
        {
            Autosave();
        }
    }

    private void OnShopRequested()
    {
        if (!CanHandlePanelInteractionRequest(_activePanelMode, PanelMode.Shop))
        {
            TryNotifyBlockedWorldInteraction(PanelMode.Shop);
            return;
        }

        if (_inventory is null || _wallet is null)
        {
            return;
        }

        var nextMode = ResolvePanelModeAfterInteractionRequest(_activePanelMode, PanelMode.Shop);
        if (nextMode == PanelMode.Shop)
        {
            _ = TryApplyShopOpenSideEffects(_inventory, _wallet, _shopOffers);
        }

        RenderPanels();
        SetActivePanelMode(nextMode);
        if (nextMode == PanelMode.Shop)
        {
            SetPanelContextFarmStatus(BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        }
    }

    private void OnStorageRequested()
    {
        if (!CanHandlePanelInteractionRequest(_activePanelMode, PanelMode.Storage))
        {
            TryNotifyBlockedWorldInteraction(PanelMode.Storage);
            return;
        }

        var nextMode = ResolvePanelModeAfterInteractionRequest(_activePanelMode, PanelMode.Storage);
        RenderPanels();
        SetActivePanelMode(nextMode);
        if (nextMode == PanelMode.Storage && _inventory is not null && _storage is not null)
        {
            SetPanelContextFarmStatus(BuildStorageBrowseStatusMessage(_inventory, _storage, _itemCatalog));
        }
    }

    private void OnRequestBoardRequested()
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_inventory is null || _wallet is null)
        {
            return;
        }

        var changed = TryCompleteNextRequest(_requests, _requestBoardService, _inventory, _completedRequestIds, _wallet, _itemCatalog, out var message);
        RefreshRequestBoardStatus(message);
        SetFarmStatus(message);
        RenderPanels();

        if (changed)
        {
            RefreshHud();
            Autosave();
        }
    }

    private void OnShopBuyRequested()
    {
        if (_inventory is null || _wallet is null || !TryGetSelectedShopOffer(out var offer) || offer is null)
        {
            return;
        }

        var changed = _shopService.TryPurchase(_inventory, _wallet, offer, 1);
        if (changed)
        {
            RefreshHud();
            Autosave();
        }

        var actionMessage = BuildShopPurchaseStatusMessage(offer, _inventory, _wallet, changed, _itemCatalog);
        SetPanelContextFarmStatus(BuildShopActionStatusMessage(actionMessage, _shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnShopSellRequested()
    {
        if (_inventory is null || _wallet is null || !TryGetSelectedShopOffer(out var offer) || offer is null)
        {
            return;
        }

        var changed = _shopService.TrySell(_inventory, _wallet, offer, 1);
        if (changed)
        {
            RefreshHud();
            Autosave();
        }

        var actionMessage = BuildShopSellStatusMessage(offer, _inventory, changed, _itemCatalog);
        SetPanelContextFarmStatus(BuildShopActionStatusMessage(actionMessage, _shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnShopPreviousOfferRequested()
    {
        if (_shopOffers.Count == 0)
        {
            return;
        }

        _selectedShopOfferIndex = (_selectedShopOfferIndex - 1 + _shopOffers.Count) % _shopOffers.Count;
        RenderPanels();
        if (_inventory is not null && _wallet is not null)
        {
            SetPanelContextFarmStatus(BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        }
    }

    private void OnShopNextOfferRequested()
    {
        if (_shopOffers.Count == 0)
        {
            return;
        }

        _selectedShopOfferIndex = (_selectedShopOfferIndex + 1) % _shopOffers.Count;
        RenderPanels();
        if (_inventory is not null && _wallet is not null)
        {
            SetPanelContextFarmStatus(BuildShopBrowseStatusMessage(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog));
        }
    }

    private void OnShopCloseRequested()
    {
        SetActivePanelMode(PanelMode.None);
    }

    private void OnStorageStoreRequested(string itemId)
    {
        if (_inventory is null || _storage is null)
        {
            return;
        }

        var changed = TryTransferItem(_inventory, _storage, itemId, 1);
        if (changed)
        {
            Autosave();
        }

        var actionMessage = BuildStorageTransferStatusMessage(itemId, changed, intoStorage: true, _inventory, _storage, _itemCatalog);
        SetPanelContextFarmStatus(BuildStorageActionStatusMessage(actionMessage, _inventory, _storage, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnStorageWithdrawRequested(string itemId)
    {
        if (_inventory is null || _storage is null)
        {
            return;
        }

        var changed = TryTransferItem(_storage, _inventory, itemId, 1);
        if (changed)
        {
            Autosave();
        }

        var actionMessage = BuildStorageTransferStatusMessage(itemId, changed, intoStorage: false, _storage, _inventory, _itemCatalog);
        SetPanelContextFarmStatus(BuildStorageActionStatusMessage(actionMessage, _inventory, _storage, _itemCatalog));
        RenderPanels();
        RefreshRequestBoardStatus();
    }

    private void OnStorageCloseRequested()
    {
        SetActivePanelMode(PanelMode.None);
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
        if (_farmGrid is not null)
        {
            SyncFarmGridLocksFromUnlockState(_farmGrid, _unlockState);
        }

        RefreshHud();
        RenderFarmPlots();
        Autosave();
        return true;
    }

    private bool TryGetSelectedShopOffer(out ShopOffer? offer)
    {
        offer = null;
        if (_shopOffers.Count == 0)
        {
            return false;
        }

        if (_selectedShopOfferIndex < 0 || _selectedShopOfferIndex >= _shopOffers.Count)
        {
            _selectedShopOfferIndex = 0;
        }

        offer = _shopOffers[_selectedShopOfferIndex];
        return true;
    }

    private void EndDay()
    {
        if (BlocksWorldInteractions(_activePanelMode))
        {
            TryNotifyBlockedWorldInteraction();
            return;
        }

        if (_clock is null || _stamina is null || _growth is null || _farmGrid is null)
        {
            return;
        }

        var rolled = ProcessDayEnd(_clock, _stamina, _growth, _farmGrid);
        if (rolled)
        {
            RefreshHud();
            RenderFarmPlots();
            SetFarmStatus("A new day begins. Water planted crops to keep them growing.");
            Autosave();
        }
    }

    private void SetActivePanelMode(PanelMode mode)
    {
        var previousMode = _activePanelMode;
        _activePanelMode = mode;
        ApplyPanelVisibility();

        if (mode == PanelMode.None && previousMode != PanelMode.None)
        {
            SetFarmStatus(BuildPanelCloseStatusMessage(previousMode, _latestPanelContextFarmStatusMessage));
            _latestPanelContextFarmStatusMessage = string.Empty;
            return;
        }

        if (previousMode == PanelMode.None && mode != PanelMode.None)
        {
            _latestPanelContextFarmStatusMessage = string.Empty;
        }

        var statusMessage = BuildPanelModeStatusMessage(previousMode, mode);
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            SetFarmStatus(statusMessage);
        }
    }

    private void ApplyPanelVisibility()
    {
        var visibility = ResolvePanelVisibility(_activePanelMode);

        if (_inventoryPanel is not null)
        {
            _inventoryPanel.Visible = visibility.InventoryVisible;
        }

        if (_shopPanel is not null)
        {
            _shopPanel.Visible = visibility.ShopVisible;
        }

        if (_storagePanel is not null)
        {
            _storagePanel.Visible = visibility.StorageVisible;
        }
    }

    private void RenderFarmPlots()
    {
        if (_farmGrid is null)
        {
            return;
        }

        foreach (var plotNode in _farmPlotNodes)
        {
            var plot = _farmGrid.GetPlot(plotNode.GridX, plotNode.GridY);
            plotNode.Render(plot, ResolveCropDisplayName(plot), GetLockedPlotHint(plot.X, plot.Y));
        }
    }

    private void RenderPanels()
    {
        if (_inventory is not null)
        {
            _inventoryPanel?.Render(_inventory, _itemCatalog);
        }

        _shopPanel?.Render(_shopOffers, _selectedShopOfferIndex, _inventory, _wallet, _itemCatalog);

        if (_inventory is not null && _storage is not null)
        {
            _storagePanel?.Render(_inventory, _storage, _itemCatalog);
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
            CreatePlotSnapshots(_farmGrid, _unlockState),
            _unlockState.UnlockedPlotKeys.OrderBy(static key => key).ToList(),
            _completedRequestIds.OrderBy(static id => id).ToList());

        var savePath = GetSaveSlotPath();
        var saveDir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrWhiteSpace(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        File.WriteAllText(savePath, SaveGameStore.Serialize(snapshot));
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

    private void RefreshRequestBoardStatus(string? overrideMessage = null)
    {
        if (_requestStatusLabel is null || _inventory is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(overrideMessage))
        {
            _requestStatusLabel.Text = overrideMessage;
            return;
        }

        _requestStatusLabel.Text = BuildRequestBoardStatusText(_requests, _completedRequestIds, _inventory, _itemCatalog);
    }

    private void SetFarmStatus(string message)
    {
        _persistedFarmStatusMessage = message;
        if (_farmStatusLabel is not null)
        {
            _farmStatusLabel.Text = message;
        }
    }

    private void SetPanelContextFarmStatus(string message)
    {
        _latestPanelContextFarmStatusMessage = message;
        SetFarmStatus(message);
    }

    private void PreviewFarmStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || _farmStatusLabel is null)
        {
            return;
        }

        _farmStatusLabel.Text = message;
    }

    private void RestoreFarmStatus()
    {
        if (_farmStatusLabel is null || string.IsNullOrWhiteSpace(_persistedFarmStatusMessage))
        {
            return;
        }

        _farmStatusLabel.Text = _persistedFarmStatusMessage;
    }

    private bool TryNotifyBlockedWorldInteraction(PanelMode requestedMode = PanelMode.None)
    {
        var message = BuildBlockedWorldInteractionMessage(_activePanelMode, requestedMode);
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        SetFarmStatus(message);
        return true;
    }

    private string? ResolveCropDisplayName(PlotState plot)
    {
        if (plot.Crop is null)
        {
            return null;
        }

        return _cropCatalog.TryGetValue(plot.Crop.CropId, out var crop) ? crop.DisplayName : plot.Crop.CropId;
    }

    private RuntimeState CreateDefaultRuntimeState()
    {
        _unlockState.UnlockedPlotKeys.Clear();
        foreach (var plotKey in DefaultUnlockedPlotKeys)
        {
            _unlockState.UnlockedPlotKeys.Add(plotKey);
        }

        _completedRequestIds.Clear();

        var inventory = new InventoryState(DefaultInventorySlots, DefaultMaxStackSize);
        inventory.TryAdd("parsnip_seed", 4);

        var storage = new InventoryState(DefaultStorageSlots, DefaultMaxStackSize);
        storage.TryAdd("wood", 12);

        var farmGrid = new FarmGrid(DefaultFarmWidth, DefaultFarmHeight);
        SyncFarmGridLocksFromUnlockState(farmGrid, _unlockState);

        return new RuntimeState(
            new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60),
            new StaminaState(maximum: 100, current: 100),
            new Wallet(200),
            inventory,
            storage,
            farmGrid);
    }

    private static string GetSaveSlotPath()
    {
        return ProjectSettings.GlobalizePath("user://saves/slot-1.json");
    }
}
