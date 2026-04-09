using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using HarvestManor.World;
using Godot;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class GameBootstrapIntegrationTests
{
    [Fact]
    public void TryLoadSnapshotFromPath_RestoresSavedProgressIntoRuntimeState()
    {
        var snapshot = new SaveGameSnapshot(
            new GameDate(Season.Spring, 5),
            MinuteOfDay: 540,
            Gold: 321,
            Stamina: 77,
            Inventory: new List<ItemStack> { new("parsnip_crop", 3) },
            Storage: new List<ItemStack> { new("wood", 18) },
            Plots: new List<PlotSnapshot> { new(0, 0, true, false, false, true, "parsnip", 4) },
            UnlockedPlotKeys: new List<string> { "0,0", "1,0", "2,0" },
            CompletedRequests: new List<string> { "ship_5_parsnips" });

        var savePath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(savePath, SaveGameStore.Serialize(snapshot));

            Assert.True(GameBootstrap.TryLoadSnapshotFromPath(savePath, out var loadedSnapshot));

            var unlockState = new UnlockState(new HashSet<string>());
            var completedRequests = new HashSet<string>();
            var state = GameBootstrap.CreateRuntimeStateFromSnapshot(loadedSnapshot!, unlockState, completedRequests);

            Assert.Equal(new GameDate(Season.Spring, 5), state.Clock.Date);
            Assert.Equal(540, state.Clock.CurrentMinuteOfDay);
            Assert.Equal(321, state.Wallet.Gold);
            Assert.Equal(77, state.Stamina.Current);
            Assert.Equal(3, state.Inventory.GetQuantity("parsnip_crop"));
            Assert.Equal(18, state.Storage.GetQuantity("wood"));
            Assert.Contains("2,0", unlockState.UnlockedPlotKeys);
            Assert.Contains("ship_5_parsnips", completedRequests);

            var plot = state.FarmGrid.GetPlot(0, 0);
            Assert.True(plot.IsTilled);
            Assert.True(plot.IsHarvestReady);
            Assert.Equal("parsnip", plot.Crop!.CropId);
            Assert.Equal(4, plot.Crop.DaysGrown);
        }
        finally
        {
            File.Delete(savePath);
        }
    }

    [Fact]
    public void CreateRuntimeStateFromSnapshot_FallsBackToDefaultUnlockedPlotsWhenLegacySnapshotHasNoUnlockHistory()
    {
        var snapshot = new SaveGameSnapshot(
            new GameDate(Season.Spring, 2),
            MinuteOfDay: 420,
            Gold: 200,
            Stamina: 100,
            Inventory: new List<ItemStack>(),
            Storage: new List<ItemStack>(),
            Plots: new List<PlotSnapshot>
            {
                new(0, 0, true, false, false, false, "parsnip", 1),
                new(2, 0, false, false, false, false, null, 0)
            },
            UnlockedPlotKeys: new List<string>(),
            CompletedRequests: new List<string>());

        var unlockState = new UnlockState(new HashSet<string>());
        var completedRequests = new HashSet<string>();

        var state = GameBootstrap.CreateRuntimeStateFromSnapshot(snapshot, unlockState, completedRequests);

        Assert.Contains("0,0", unlockState.UnlockedPlotKeys);
        Assert.Contains("1,0", unlockState.UnlockedPlotKeys);
        Assert.Contains("0,1", unlockState.UnlockedPlotKeys);
        Assert.Contains("1,1", unlockState.UnlockedPlotKeys);
        Assert.DoesNotContain("2,0", unlockState.UnlockedPlotKeys);
        Assert.False(state.FarmGrid.GetPlot(0, 0).IsLocked);
        Assert.True(state.FarmGrid.GetPlot(2, 0).IsLocked);
    }

    [Fact]
    public void TryHandleFarmPlotInteraction_SupportsTillPlantWaterAndHarvestLoop()
    {
        var crops = CreateCropCatalog();
        var growth = new CropGrowthService(crops);
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        var stamina = new StaminaState(maximum: 100, current: 100);
        var inventory = new InventoryState(12, 99);
        var farmGrid = new FarmGrid(1, 1);

        Assert.True(inventory.TryAdd("parsnip_seed", 1));

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var tillMessage));
        Assert.Equal("Plot tilled.", tillMessage);
        Assert.True(farmGrid.GetPlot(0, 0).IsTilled);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var plantMessage));
        Assert.Equal("Planted Parsnip.", plantMessage);
        Assert.Equal(0, inventory.GetQuantity("parsnip_seed"));
        Assert.Equal("parsnip", farmGrid.GetPlot(0, 0).Crop!.CropId);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var waterMessage));
        Assert.Equal("Watered plot.", waterMessage);
        Assert.True(farmGrid.GetPlot(0, 0).IsWateredToday);

        for (var day = 1; day <= 4; day++)
        {
            Assert.True(GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid));
            if (day < 4)
            {
                Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var repeatWaterMessage));
                Assert.Equal("Watered plot.", repeatWaterMessage);
            }
        }

        Assert.True(farmGrid.GetPlot(0, 0).IsHarvestReady);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var harvestMessage));
        Assert.Equal("Harvested Parsnip.", harvestMessage);
        Assert.Equal(1, inventory.GetQuantity("parsnip_crop"));

        var harvestedPlot = farmGrid.GetPlot(0, 0);
        Assert.True(harvestedPlot.IsTilled);
        Assert.Null(harvestedPlot.Crop);
        Assert.False(harvestedPlot.IsHarvestReady);
    }

    [Fact]
    public void TryTransferItemAndCompleteNextRequest_CreatesPlayableTownLoop()
    {
        var inventory = new InventoryState(12, 99);
        var storage = new InventoryState(24, 99);
        var wallet = new Wallet(0);
        var completedRequests = new HashSet<string>();
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };

        Assert.True(inventory.TryAdd("parsnip_crop", 5));

        Assert.True(GameBootstrap.TryTransferItem(inventory, storage, "parsnip_crop", 2));
        Assert.Equal(3, inventory.GetQuantity("parsnip_crop"));
        Assert.Equal(2, storage.GetQuantity("parsnip_crop"));

        Assert.True(GameBootstrap.TryTransferItem(storage, inventory, "parsnip_crop", 2));
        Assert.Equal(5, inventory.GetQuantity("parsnip_crop"));
        Assert.Equal(0, storage.GetQuantity("parsnip_crop"));

        Assert.True(
            GameBootstrap.TryCompleteNextRequest(
                requests,
                new RequestBoardService(),
                inventory,
                completedRequests,
                wallet,
                out var completionMessage));

        Assert.Equal("Completed request ship_5_parsnips for 120g.", completionMessage);
        Assert.Equal(120, wallet.Gold);
        Assert.Equal(0, inventory.GetQuantity("parsnip_crop"));
        Assert.Contains("ship_5_parsnips", completedRequests);
    }

    [Fact]
    public void BuildRequestBoardStatusText_ReflectsCurrentInventoryAfterTransfers()
    {
        var inventory = new InventoryState(12, 99);
        var storage = new InventoryState(24, 99);
        var completedRequests = new HashSet<string>();
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };

        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        Assert.Equal(
            "Active request: parsnip_crop 5/5. Click board to turn in.",
            GameBootstrap.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        Assert.True(GameBootstrap.TryTransferItem(inventory, storage, "parsnip_crop", 2));
        Assert.Equal(
            "Active request: parsnip_crop 3/5. Click board to turn in.",
            GameBootstrap.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        completedRequests.Add("ship_5_parsnips");
        Assert.Equal(
            "All requests completed.",
            GameBootstrap.BuildRequestBoardStatusText(requests, completedRequests, inventory));
    }

    [Theory]
    [InlineData(GameBootstrap.PanelMode.None, false, false, false)]
    [InlineData(GameBootstrap.PanelMode.Shop, false, true, false)]
    [InlineData(GameBootstrap.PanelMode.Storage, true, false, true)]
    public void ResolvePanelVisibility_ReturnsExclusivePanelModes(
        GameBootstrap.PanelMode mode,
        bool inventoryVisible,
        bool shopVisible,
        bool storageVisible)
    {
        var visibility = GameBootstrap.ResolvePanelVisibility(mode);

        Assert.Equal(inventoryVisible, visibility.InventoryVisible);
        Assert.Equal(shopVisible, visibility.ShopVisible);
        Assert.Equal(storageVisible, visibility.StorageVisible);
    }

    [Theory]
    [InlineData(GameBootstrap.PanelMode.None, false)]
    [InlineData(GameBootstrap.PanelMode.Shop, true)]
    [InlineData(GameBootstrap.PanelMode.Storage, true)]
    public void BlocksWorldInteractions_ReturnsTrueOnlyWhenAPanelIsOpen(
        GameBootstrap.PanelMode mode,
        bool blocksWorldInteraction)
    {
        Assert.Equal(blocksWorldInteraction, GameBootstrap.BlocksWorldInteractions(mode));
    }

    [Theory]
    [InlineData(GameBootstrap.PanelMode.None, Key.Escape, GameBootstrap.PanelMode.None)]
    [InlineData(GameBootstrap.PanelMode.Shop, Key.Escape, GameBootstrap.PanelMode.None)]
    [InlineData(GameBootstrap.PanelMode.Storage, Key.Escape, GameBootstrap.PanelMode.None)]
    [InlineData(GameBootstrap.PanelMode.Shop, Key.F7, GameBootstrap.PanelMode.Shop)]
    public void ResolvePanelModeAfterUnhandledKey_ClosesPanelsOnlyOnEscape(
        GameBootstrap.PanelMode currentMode,
        Key keycode,
        GameBootstrap.PanelMode expectedMode)
    {
        Assert.Equal(expectedMode, GameBootstrap.ResolvePanelModeAfterUnhandledKey(currentMode, keycode));
    }

    [Theory]
    [InlineData(GameBootstrap.PanelMode.None, Key.F7, true)]
    [InlineData(GameBootstrap.PanelMode.Shop, Key.F7, false)]
    [InlineData(GameBootstrap.PanelMode.Storage, Key.F7, false)]
    [InlineData(GameBootstrap.PanelMode.Shop, Key.Escape, false)]
    public void CanTriggerDemoExpansionShortcut_OnlyWorksWithoutAnOpenPanel(
        GameBootstrap.PanelMode currentMode,
        Key keycode,
        bool canTriggerShortcut)
    {
        Assert.Equal(canTriggerShortcut, GameBootstrap.CanTriggerDemoExpansionShortcut(currentMode, keycode));
    }

    [Theory]
    [InlineData(GameBootstrap.PanelMode.None, null)]
    [InlineData(GameBootstrap.PanelMode.Shop, "Close the shop panel before interacting with the world.")]
    [InlineData(GameBootstrap.PanelMode.Storage, "Close the storage panel before interacting with the world.")]
    public void BuildBlockedWorldInteractionMessage_ProvidesActionablePanelFeedback(
        GameBootstrap.PanelMode mode,
        string? expectedMessage)
    {
        Assert.Equal(expectedMessage, GameBootstrap.BuildBlockedWorldInteractionMessage(mode));
    }

    [Fact]
    public void GetLockedPlotHint_ReturnsUnlockPromptForDemoExpansionPlot()
    {
        Assert.Equal("Click: unlock (120g)", GameBootstrap.GetLockedPlotHint(2, 0));
        Assert.Equal("Locked", GameBootstrap.GetLockedPlotHint(4, 4));
    }

    [Fact]
    public void TryHandleLockedPlotInteraction_UnlocksDemoPlotAndSpendsGold()
    {
        var expansion = new FarmExpansionService();
        var unlockState = new UnlockState(new HashSet<string> { "0,0", "1,0", "0,1", "1,1" });

        var changed = GameBootstrap.TryHandleLockedPlotInteraction(
            expansion,
            unlockState,
            currentGold: 200,
            x: 2,
            y: 0,
            out var updatedGold,
            out var message);

        Assert.True(changed);
        Assert.Equal(80, updatedGold);
        Assert.Equal("Unlocked plot (2,0) for 120g.", message);
        Assert.Contains("2,0", unlockState.UnlockedPlotKeys);
    }

    [Fact]
    public void TryHandleLockedPlotInteraction_ReturnsCostMessageWhenGoldIsInsufficient()
    {
        var expansion = new FarmExpansionService();
        var unlockState = new UnlockState(new HashSet<string> { "0,0", "1,0", "0,1", "1,1" });

        var changed = GameBootstrap.TryHandleLockedPlotInteraction(
            expansion,
            unlockState,
            currentGold: 100,
            x: 2,
            y: 0,
            out var updatedGold,
            out var message);

        Assert.False(changed);
        Assert.Equal(100, updatedGold);
        Assert.Equal("Need 120g to unlock plot (2,0).", message);
        Assert.DoesNotContain("2,0", unlockState.UnlockedPlotKeys);
    }

    [Theory]
    [InlineData(false, false, "Fresh start. Click a plot to till, plant, water, or harvest.")]
    [InlineData(true, true, "Loaded slot-1.json. Click a plot to till, plant, water, or harvest.")]
    [InlineData(true, false, "Save file was unreadable. Started a fresh day instead.")]
    public void BuildStartupFarmStatusMessage_ExplainsHowBootstrapHandledTheSave(
        bool saveFileExists,
        bool loadedExistingSave,
        string expectedMessage)
    {
        Assert.Equal(expectedMessage, GameBootstrap.BuildStartupFarmStatusMessage(saveFileExists, loadedExistingSave));
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    [InlineData(true, false, false, true)]
    public void ShouldAutosaveAfterBootstrap_RepairsUnreadableSaveSlotsAndPersistsMeaningfulChanges(
        bool saveFileExists,
        bool loadedExistingSave,
        bool hasMeaningfulStateChanges,
        bool shouldAutosave)
    {
        Assert.Equal(
            shouldAutosave,
            GameBootstrap.ShouldAutosaveAfterBootstrap(saveFileExists, loadedExistingSave, hasMeaningfulStateChanges));
    }

    private static IReadOnlyDictionary<string, CropDefinition> CreateCropCatalog()
    {
        return new Dictionary<string, CropDefinition>
        {
            ["parsnip"] = new(
                "parsnip",
                "Parsnip",
                "Spring",
                "parsnip_seed",
                "parsnip_crop",
                20,
                35,
                4,
                new[] { 1, 1, 2 })
        };
    }
}
