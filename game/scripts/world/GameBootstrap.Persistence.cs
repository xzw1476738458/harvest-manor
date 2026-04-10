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

namespace HarvestManor.World;

public partial class GameBootstrap
{
    internal static (int CropCount, int ItemCount) LoadCatalogCounts(string cropCatalogJson, string itemCatalogJson)
    {
        var loader = new ContentCatalogLoader();
        var crops = loader.ParseCropCatalogJson(cropCatalogJson, "res://data/crops/spring.json");
        var items = loader.ParseItemCatalogJson(itemCatalogJson, "res://data/items/items.json");
        return (crops.Count, items.Count);
    }

    public static bool ShouldAutosaveAfterBootstrap(bool saveFileExists, bool loadedExistingSave, bool hasMeaningfulStateChanges)
    {
        return saveFileExists && !loadedExistingSave
            || loadedExistingSave && hasMeaningfulStateChanges;
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
