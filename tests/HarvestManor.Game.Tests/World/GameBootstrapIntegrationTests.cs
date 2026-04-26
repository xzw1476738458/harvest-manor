using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Gathering;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using HarvestManor.UI;
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
            CompletedRequests: new List<string> { "ship_5_parsnips" },
            HarvestedGatheringNodeIds: new List<string>());

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
            CompletedRequests: new List<string>(),
            HarvestedGatheringNodeIds: new List<string>());

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
        Assert.Equal("Plot tilled. Click again to plant Parsnip.", tillMessage);
        Assert.True(farmGrid.GetPlot(0, 0).IsTilled);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var plantMessage));
        Assert.Equal("Planted Parsnip. Click again to water.", plantMessage);
        Assert.Equal(0, inventory.GetQuantity("parsnip_seed"));
        Assert.Equal("parsnip", farmGrid.GetPlot(0, 0).Crop!.CropId);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var waterMessage));
        Assert.Equal("Watered Parsnip.", waterMessage);
        Assert.True(farmGrid.GetPlot(0, 0).IsWateredToday);

        for (var day = 1; day <= 4; day++)
        {
            Assert.True(GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid, crops).DayRolled);
            if (day < 4)
            {
            Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var repeatWaterMessage));
            Assert.Equal("Watered Parsnip.", repeatWaterMessage);
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
    public void TryHandleFarmPlotInteraction_WhenTillingWithoutSeeds_ExplainsThatPlantingCannotContinue()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(12, 99);
        var farmGrid = new FarmGrid(1, 1);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var tillMessage));
        Assert.Equal("Plot tilled. No seeds available.", tillMessage);
        Assert.True(farmGrid.GetPlot(0, 0).IsTilled);
    }

    [Fact]
    public void TryHandleFarmPlotInteraction_WhenHarvestIsBlockedByFullInventory_NamesTheCrop()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(1, 1);
        var farmGrid = new FarmGrid(1, 1);
        Assert.True(inventory.TryAdd("wood", 1));
        farmGrid.SetPlot(new PlotState(0, 0, true, false, true, true, new CropInstance("parsnip", 4)));

        Assert.False(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var message));
        Assert.Equal("Cannot harvest Parsnip: inventory full.", message);
        Assert.Equal("parsnip", farmGrid.GetPlot(0, 0).Crop!.CropId);
        Assert.True(farmGrid.GetPlot(0, 0).IsHarvestReady);
    }

    [Fact]
    public void TryHandleFarmPlotInteraction_WhenWateringNamesTheCropAndRepeatedWateringKeepsContext()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(12, 99);
        var farmGrid = new FarmGrid(1, 1);
        farmGrid.SetPlot(new PlotState(0, 0, true, false, false, false, new CropInstance("parsnip", 2)));

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var firstMessage));
        Assert.Equal("Watered Parsnip.", firstMessage);
        Assert.True(farmGrid.GetPlot(0, 0).IsWateredToday);

        Assert.False(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var repeatMessage));
        Assert.Equal("Parsnip already watered today.", repeatMessage);
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
                null,
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

        Assert.Equal(
            "Active request: parsnip_crop 0/5. Need 5 more.",
            StatusMessageBuilder.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        Assert.Equal(
            "Request ready: parsnip_crop 5/5. Click board to turn in.",
            StatusMessageBuilder.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        Assert.True(GameBootstrap.TryTransferItem(inventory, storage, "parsnip_crop", 2));
        Assert.Equal(
            "Active request: parsnip_crop 3/5. Need 2 more.",
            StatusMessageBuilder.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        completedRequests.Add("ship_5_parsnips");
        Assert.Equal(
            "All requests completed.",
            StatusMessageBuilder.BuildRequestBoardStatusText(requests, completedRequests, inventory));
    }

    [Theory]
    [InlineData("Active request: Parsnip 0/5. Need 5 more.", 0)]
    [InlineData("Request ready: Parsnip 5/5. Click board to turn in.", 5)]
    public void BuildRequestBoardStatusText_UsesDisplayNamesWhenCatalogIsAvailable(string expectedMessage, int quantity)
    {
        var inventory = new InventoryState(12, 99);
        if (quantity > 0)
        {
            Assert.True(inventory.TryAdd("parsnip_crop", quantity));
        }

        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };

        Assert.Equal(
            expectedMessage,
            StatusMessageBuilder.BuildRequestBoardStatusText(requests, new HashSet<string>(), inventory, CreateItemCatalog()));
    }

    [Theory]
    [InlineData(PanelMode.None, false, false, false)]
    [InlineData(PanelMode.Shop, false, true, false)]
    [InlineData(PanelMode.Storage, true, false, true)]
    [InlineData(PanelMode.Inventory, true, false, false)]
    public void ResolvePanelVisibility_ReturnsExclusivePanelModes(
        PanelMode mode,
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
    [InlineData(PanelMode.None, false)]
    [InlineData(PanelMode.Shop, true)]
    [InlineData(PanelMode.Storage, true)]
    [InlineData(PanelMode.Inventory, true)]
    public void BlocksWorldInteractions_ReturnsTrueOnlyWhenAPanelIsOpen(
        PanelMode mode,
        bool blocksWorldInteraction)
    {
        Assert.Equal(blocksWorldInteraction, GameBootstrap.BlocksWorldInteractions(mode));
    }

    [Theory]
    [InlineData(PanelMode.None, false)]
    [InlineData(PanelMode.Shop, false)]
    [InlineData(PanelMode.Storage, false)]
    [InlineData(PanelMode.Inventory, true)]
    public void ShouldSilenceHoverPreview_OnlyMutesHoverHintsWhileTheInventoryIsOpen(
        PanelMode mode,
        bool shouldSilence)
    {
        Assert.Equal(shouldSilence, GameBootstrap.ShouldSilenceHoverPreview(mode));
    }

    [Theory]
    [InlineData(PanelMode.None, Key.Escape, PanelMode.None)]
    [InlineData(PanelMode.Shop, Key.Escape, PanelMode.None)]
    [InlineData(PanelMode.Storage, Key.Escape, PanelMode.None)]
    [InlineData(PanelMode.Inventory, Key.Escape, PanelMode.None)]
    [InlineData(PanelMode.Shop, Key.F7, PanelMode.Shop)]
    [InlineData(PanelMode.None, Key.Tab, PanelMode.Inventory)]
    [InlineData(PanelMode.Inventory, Key.Tab, PanelMode.None)]
    [InlineData(PanelMode.Shop, Key.Tab, PanelMode.Shop)]
    [InlineData(PanelMode.Storage, Key.Tab, PanelMode.Storage)]
    public void ResolvePanelModeAfterUnhandledKey_ClosesPanelsOnlyOnEscape(
        PanelMode currentMode,
        Key keycode,
        PanelMode expectedMode)
    {
        Assert.Equal(expectedMode, GameBootstrap.ResolvePanelModeAfterUnhandledKey(currentMode, keycode));
    }

    [Theory]
    [InlineData(PanelMode.None, PanelMode.Shop, true)]
    [InlineData(PanelMode.None, PanelMode.Storage, true)]
    [InlineData(PanelMode.Shop, PanelMode.Shop, true)]
    [InlineData(PanelMode.Storage, PanelMode.Storage, true)]
    [InlineData(PanelMode.Shop, PanelMode.Storage, false)]
    [InlineData(PanelMode.Storage, PanelMode.Shop, false)]
    public void CanHandlePanelInteractionRequest_AllowsOpeningAndClosingTheRequestedPanelOnly(
        PanelMode currentMode,
        PanelMode requestedMode,
        bool canHandleRequest)
    {
        Assert.Equal(canHandleRequest, GameBootstrap.CanHandlePanelInteractionRequest(currentMode, requestedMode));
    }

    [Theory]
    [InlineData(PanelMode.None, PanelMode.Shop, PanelMode.Shop)]
    [InlineData(PanelMode.None, PanelMode.Storage, PanelMode.Storage)]
    [InlineData(PanelMode.Shop, PanelMode.Shop, PanelMode.None)]
    [InlineData(PanelMode.Storage, PanelMode.Storage, PanelMode.None)]
    public void ResolvePanelModeAfterInteractionRequest_TogglesTheRequestedPanel(
        PanelMode currentMode,
        PanelMode requestedMode,
        PanelMode expectedMode)
    {
        Assert.Equal(expectedMode, GameBootstrap.ResolvePanelModeAfterInteractionRequest(currentMode, requestedMode));
    }

    [Theory]
    [InlineData(PanelMode.None, Key.F7, true)]
    [InlineData(PanelMode.Shop, Key.F7, false)]
    [InlineData(PanelMode.Storage, Key.F7, false)]
    [InlineData(PanelMode.Shop, Key.Escape, false)]
    public void CanTriggerQuickExpansionShortcut_OnlyWorksWithoutAnOpenPanel(
        PanelMode currentMode,
        Key keycode,
        bool canTriggerShortcut)
    {
        Assert.Equal(canTriggerShortcut, GameBootstrap.CanTriggerQuickExpansionShortcut(currentMode, keycode));
    }

    [Theory]
    [InlineData(PanelMode.None, null)]
    [InlineData(PanelMode.Shop, "Close the shop panel before interacting with the world.")]
    [InlineData(PanelMode.Storage, "Close the storage panel before interacting with the world.")]
    [InlineData(PanelMode.Inventory, "Close the inventory before interacting with the world.")]
    public void BuildBlockedWorldInteractionMessage_ProvidesActionablePanelFeedback(
        PanelMode mode,
        string? expectedMessage)
    {
        Assert.Equal(expectedMessage, StatusMessageBuilder.BuildBlockedWorldInteractionMessage(mode));
    }

    [Theory]
    [InlineData(PanelMode.Shop, PanelMode.Shop, "Shop open. Click again or press Esc to close.")]
    [InlineData(PanelMode.Storage, PanelMode.Storage, "Storage open. Click again or press Esc to close.")]
    [InlineData(PanelMode.Shop, PanelMode.Storage, "Close the shop panel before opening storage.")]
    [InlineData(PanelMode.Storage, PanelMode.Shop, "Close the storage panel before opening shop.")]
    [InlineData(PanelMode.Shop, PanelMode.None, "Close the shop panel before interacting with the world.")]
    [InlineData(PanelMode.Storage, PanelMode.None, "Close the storage panel before interacting with the world.")]
    [InlineData(PanelMode.Inventory, PanelMode.Inventory, "Inventory open. Press Tab or Esc to close.")]
    [InlineData(PanelMode.Inventory, PanelMode.None, "Close the inventory before interacting with the world.")]
    public void BuildBlockedWorldInteractionMessage_UsesRequestedPanelContextWhenAvailable(
        PanelMode currentMode,
        PanelMode requestedMode,
        string expectedMessage)
    {
        Assert.Equal(expectedMessage, StatusMessageBuilder.BuildBlockedWorldInteractionMessage(currentMode, requestedMode));
    }

    [Theory]
    [InlineData(PanelMode.None, PanelMode.None, null)]
    [InlineData(PanelMode.None, PanelMode.Shop, "Shop open. Use Buy/Sell or press Esc to close.")]
    [InlineData(PanelMode.None, PanelMode.Storage, "Storage open. Move items or press Esc to close.")]
    [InlineData(PanelMode.None, PanelMode.Inventory, "Inventory open. Press Tab or Esc to close.")]
    [InlineData(PanelMode.Shop, PanelMode.None, null)]
    [InlineData(PanelMode.Storage, PanelMode.None, null)]
    [InlineData(PanelMode.Inventory, PanelMode.None, null)]
    public void BuildPanelModeStatusMessage_ExplainsPanelOpenAndCloseFlow(
        PanelMode previousMode,
        PanelMode nextMode,
        string? expectedMessage)
    {
        Assert.Equal(expectedMessage, StatusMessageBuilder.BuildPanelModeStatusMessage(previousMode, nextMode));
    }

    [Theory]
    [InlineData(PanelMode.Shop, "Shop selection: parsnip_seed. Ready to buy 1.", "Shop selection: parsnip_seed. Ready to buy 1.")]
    [InlineData(PanelMode.Storage, "Stored 1 parsnip_seed. Storage selection: store parsnip_seed or take wood.", "Stored 1 parsnip_seed. Storage selection: store parsnip_seed or take wood.")]
    [InlineData(PanelMode.Shop, null, "Panels closed. Interact with the world again.")]
    public void BuildPanelCloseStatusMessage_RestoresTheLatestPanelContextWhenAvailable(
        PanelMode previousMode,
        string? latestPanelContextMessage,
        string expectedMessage)
    {
        Assert.Equal(expectedMessage, StatusMessageBuilder.BuildPanelCloseStatusMessage(previousMode, latestPanelContextMessage));
    }

    [Fact]
    public void BuildFarmPlotHoverStatusMessage_ExplainsTheCurrentPlotActionBeforeClick()
    {
        var crops = CreateCropCatalog();

        Func<int, int, int?> lookup = (x, y) => x == 2 && y == 0 ? 120 : null;
        Assert.Equal(
            "Hover plot: unlock for 120g.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(new PlotState(2, 0, false, true, false, false, null), crops, lookupUnlockCost: lookup));
        Assert.Equal(
            "Hover plot: click to till.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(PlotState.Wild(0, 0), crops));
        Assert.Equal(
            "Hover plot: click to plant.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(PlotState.Tilled(0, 0), crops));
        Assert.Equal(
            "Hover Parsnip: click to water.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, false, false, new CropInstance("parsnip", 2)),
                crops));
        Assert.Equal(
            "Hover Parsnip: watered today.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, true, false, new CropInstance("parsnip", 2)),
                crops));
        Assert.Equal(
            "Hover Parsnip: ready to harvest.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, true, true, new CropInstance("parsnip", 4)),
                crops));
    }

    [Fact]
    public void BuildFarmPlotHoverStatusMessage_ReflectsCurrentResourcesBeforeClick()
    {
        var crops = CreateCropCatalog();

        var emptyInventory = new InventoryState(12, 99);
        Assert.Equal(
            "Hover plot: no seeds available.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                PlotState.Tilled(0, 0),
                crops,
                emptyInventory,
                currentGold: 200));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        Assert.Equal(
            "Hover Parsnip: inventory full.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, true, true, new CropInstance("parsnip", 4)),
                crops,
                fullInventory,
                currentGold: 200));

        Func<int, int, int?> lookup = (x, y) => x == 2 && y == 0 ? 120 : null;
        Assert.Equal(
            "Hover plot: need 120g to unlock.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                new PlotState(2, 0, false, true, false, false, null),
                crops,
                emptyInventory,
                currentGold: 100,
                lookupUnlockCost: lookup));
    }

    [Fact]
    public void BuildFarmPlotHoverStatusMessage_PreviewsTheSameAutoSelectedCropThatWillBePlanted()
    {
        var crops = CreateMultiCropCatalog();
        var inventory = new InventoryState(12, 99);
        var farmGrid = new FarmGrid(1, 1);
        farmGrid.SetPlot(PlotState.Tilled(0, 0));
        Assert.True(inventory.TryAdd("potato_seed", 1));
        Assert.True(inventory.TryAdd("parsnip_seed", 1));

        Assert.Equal(
            "Hover plot: click to plant Parsnip.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                farmGrid.GetPlot(0, 0),
                crops,
                inventory,
                currentGold: 200));

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var plantMessage));
        Assert.Equal("Planted Parsnip. Click again to water.", plantMessage);
        Assert.Equal("parsnip", farmGrid.GetPlot(0, 0).Crop!.CropId);
    }

    [Theory]
    [InlineData("bed", "click to end day", "Hover bed: click to end day.")]
    [InlineData("shop", "buy or sell items", "Hover shop: buy or sell items.")]
    [InlineData("request board", "turn in crops", "Hover request board: turn in crops.")]
    public void BuildInteractionHoverStatusMessage_DescribesTheInteractionBeforeClick(
        string interactionName,
        string actionDescription,
        string expectedMessage)
    {
        Assert.Equal(expectedMessage, StatusMessageBuilder.BuildInteractionHoverStatusMessage(interactionName, actionDescription));
    }

    [Fact]
    public void BuildRequestBoardHoverStatusMessage_ReflectsCurrentTurnInProgress()
    {
        var inventory = new InventoryState(12, 99);
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var completedRequests = new HashSet<string>();

        Assert.Equal(
            "Hover request board: parsnip_crop 0/5. Need 5 more.",
            StatusMessageBuilder.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));

        Assert.True(inventory.TryAdd("parsnip_crop", 3));
        Assert.Equal(
            "Hover request board: parsnip_crop 3/5. Need 2 more.",
            StatusMessageBuilder.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));

        Assert.True(GameBootstrap.TryTransferItem(inventory, new InventoryState(24, 99), "parsnip_crop", 3));
        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        Assert.Equal(
            "Hover request board: parsnip_crop 5/5 ready to turn in for 120g.",
            StatusMessageBuilder.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));

        completedRequests.Add("ship_5_parsnips");
        Assert.Equal(
            "Hover request board: all requests completed.",
            StatusMessageBuilder.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));
    }

    [Fact]
    public void BuildRequestBoardHoverStatusMessage_UsesDisplayNamesWhenCatalogIsAvailable()
    {
        var inventory = new InventoryState(12, 99);
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };

        Assert.Equal(
            "Hover request board: Parsnip 0/5. Need 5 more.",
            StatusMessageBuilder.BuildRequestBoardHoverStatusMessage(requests, new HashSet<string>(), inventory, CreateItemCatalog()));
    }

    [Fact]
    public void BuildShopBrowseStatusMessage_ReflectsTheSelectedOfferState()
    {
        var offers = new[]
        {
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            new ShopOffer("parsnip_crop", BuyPrice: 0, SellPrice: 35)
        };

        var inventory = new InventoryState(12, 99);
        var wallet = new Wallet(200);
        Assert.Equal(
            "Shop selection: parsnip_seed. Ready to buy 1.",
            StatusMessageBuilder.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, inventory, wallet));

        Assert.True(inventory.TryAdd("parsnip_crop", 1));
        Assert.Equal(
            "Shop selection: parsnip_crop. Ready to sell 1.",
            StatusMessageBuilder.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 1, inventory, wallet));

        var ownedSeeds = new InventoryState(1, 1);
        Assert.True(ownedSeeds.TryAdd("parsnip_seed", 1));
        Assert.Equal(
            "Shop selection: parsnip_seed. Ready to sell 1. Cannot buy 1: inventory full.",
            StatusMessageBuilder.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, ownedSeeds, wallet));

        var poorWallet = new Wallet(5);
        var seedInventory = new InventoryState(12, 99);
        Assert.True(seedInventory.TryAdd("parsnip_seed", 1));
        Assert.Equal(
            "Shop selection: parsnip_seed. Ready to sell 1. Need 15g more to buy 1.",
            StatusMessageBuilder.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, seedInventory, poorWallet));
    }

    [Fact]
    public void ShopStatusBuilders_UseDisplayNamesWhenCatalogIsAvailable()
    {
        var offers = new[]
        {
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10)
        };
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        var wallet = new Wallet(180);
        var itemCatalog = CreateItemCatalog();

        Assert.Equal(
            "Shop selection: Parsnip Seeds. Ready to buy or sell 1.",
            StatusMessageBuilder.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, inventory, wallet, itemCatalog));
        Assert.Equal(
            "Bought 1 Parsnip Seeds for 20g. Shop selection: Parsnip Seeds. Ready to buy or sell 1.",
            StatusMessageBuilder.BuildShopActionStatusMessage(
                "Bought 1 Parsnip Seeds for 20g.",
                offers,
                selectedOfferIndex: 0,
                inventory,
                wallet,
                itemCatalog));
        Assert.Equal(
            "Need 15g more to buy 1 Parsnip Seeds.",
            StatusMessageBuilder.BuildShopPurchaseStatusMessage(offers[0], inventory, new Wallet(5), changed: false, itemCatalog));
    }

    [Fact]
    public void BuildStorageBrowseStatusMessage_ReflectsCurrentTransferCandidates()
    {
        var inventory = new InventoryState(12, 99);
        var storage = new InventoryState(24, 99);

        Assert.True(inventory.TryAdd("parsnip_seed", 2));
        Assert.True(storage.TryAdd("wood", 1));

        Assert.Equal(
            "Storage selection: store parsnip_seed or take wood.",
            StatusMessageBuilder.BuildStorageBrowseStatusMessage(inventory, storage));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("stone", 1));
        var stockedStorage = new InventoryState(24, 99);
        Assert.True(stockedStorage.TryAdd("wood", 1));
        Assert.Equal(
            "Storage selection: store stone. Cannot take wood: inventory is full.",
            StatusMessageBuilder.BuildStorageBrowseStatusMessage(fullInventory, stockedStorage));

        var blockedInventory = new InventoryState(1, 1);
        Assert.True(blockedInventory.TryAdd("stone", 1));
        var blockedStorage = new InventoryState(1, 1);
        Assert.True(blockedStorage.TryAdd("wood", 1));
        Assert.Equal(
            "Storage blocked: cannot store stone (storage full) or take wood (inventory full).",
            StatusMessageBuilder.BuildStorageBrowseStatusMessage(blockedInventory, blockedStorage));
    }

    [Fact]
    public void StorageStatusBuilders_UseDisplayNamesWhenCatalogIsAvailable()
    {
        var inventory = new InventoryState(12, 99);
        var storage = new InventoryState(24, 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        Assert.True(storage.TryAdd("wood", 1));
        var itemCatalog = CreateItemCatalog();

        Assert.Equal(
            "Storage selection: store Parsnip Seeds or take Wood.",
            StatusMessageBuilder.BuildStorageBrowseStatusMessage(inventory, storage, itemCatalog));
        Assert.Equal(
            "Stored 1 Parsnip Seeds. Storage selection: store Parsnip Seeds or take Wood.",
            StatusMessageBuilder.BuildStorageActionStatusMessage("Stored 1 Parsnip Seeds.", inventory, storage, itemCatalog));
        Assert.Equal(
            "Cannot take Stone: inventory is full.",
            StatusMessageBuilder.BuildStorageTransferStatusMessage(
                "stone",
                changed: false,
                intoStorage: false,
                CreateStockedStorageWithStone(),
                CreateFullInventoryForStoneBlock(),
                itemCatalog));
    }

    [Fact]
    public void BuildShopActionStatusMessage_PreservesOutcomeAndCurrentSelectionContext()
    {
        var offers = new[]
        {
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10)
        };
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        var wallet = new Wallet(180);

        Assert.Equal(
            "Bought 1 parsnip_seed for 20g. Shop selection: parsnip_seed. Ready to buy or sell 1.",
            StatusMessageBuilder.BuildShopActionStatusMessage(
                "Bought 1 parsnip_seed for 20g.",
                offers,
                selectedOfferIndex: 0,
                inventory,
                wallet));
    }

    [Fact]
    public void BuildStorageActionStatusMessage_PreservesOutcomeAndCurrentTransferContext()
    {
        var inventory = new InventoryState(12, 99);
        var storage = new InventoryState(24, 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        Assert.True(storage.TryAdd("wood", 1));

        Assert.Equal(
            "Stored 1 parsnip_seed. Storage selection: store parsnip_seed or take wood.",
            StatusMessageBuilder.BuildStorageActionStatusMessage(
                "Stored 1 parsnip_seed.",
            inventory,
            storage));
    }

    [Fact]
    public void BuildRequestBoardActionStatusMessage_PreservesOutcomeAndCurrentRequestContextAfterCompletion()
    {
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120),
            new RequestDefinition("ship_3_potatoes", "potato_crop", 3, 90)
        };
        var completedRequests = new HashSet<string> { "ship_5_parsnips" };
        var inventory = new InventoryState(12, 99);

        Assert.Equal(
            "Completed request: delivered 5 Parsnip for 120g. Active request: Potato 0/3. Need 3 more.",
            StatusMessageBuilder.BuildRequestBoardActionStatusMessage(
                "Completed request: delivered 5 Parsnip for 120g.",
                requests,
                completedRequests,
                inventory,
                CreateItemCatalog()));
    }

    [Fact]
    public void BuildRequestBoardActionStatusMessage_UsesCurrentRequestContextAfterFailedTurnIn()
    {
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var completedRequests = new HashSet<string>();
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 3));

        Assert.Equal(
            "Active request: Parsnip 3/5. Need 2 more.",
            StatusMessageBuilder.BuildRequestBoardActionStatusMessage(
                "Need 2 more Parsnip.",
                requests,
                completedRequests,
                inventory,
                CreateItemCatalog()));
    }

    [Fact]
    public void BuildShopPurchaseStatusMessage_ExplainsPurchaseOutcome()
    {
        var offer = new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10);
        var wallet = new Wallet(200);
        var inventory = new InventoryState(12, 99);

        Assert.Equal(
            "Bought 1 parsnip_seed for 20g.",
            StatusMessageBuilder.BuildShopPurchaseStatusMessage(offer, inventory, wallet, changed: true));

        var poorWallet = new Wallet(5);
        Assert.Equal(
            "Need 15g more to buy 1 parsnip_seed.",
            StatusMessageBuilder.BuildShopPurchaseStatusMessage(offer, inventory, poorWallet, changed: false));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        Assert.Equal(
            "Cannot buy parsnip_seed: inventory full.",
            StatusMessageBuilder.BuildShopPurchaseStatusMessage(offer, fullInventory, wallet, changed: false));
    }

    [Fact]
    public void BuildShopSellStatusMessage_ExplainsSellOutcome()
    {
        var offer = new ShopOffer("parsnip_crop", BuyPrice: 20, SellPrice: 35);
        var stockedInventory = new InventoryState(12, 99);
        Assert.True(stockedInventory.TryAdd("parsnip_crop", 1));

        Assert.Equal(
            "Sold 1 parsnip_crop for 35g.",
            StatusMessageBuilder.BuildShopSellStatusMessage(offer, stockedInventory, changed: true));

        var emptyInventory = new InventoryState(12, 99);
        Assert.Equal(
            "Cannot sell parsnip_crop: none available.",
            StatusMessageBuilder.BuildShopSellStatusMessage(offer, emptyInventory, changed: false));
    }

    [Fact]
    public void BuildStorageTransferStatusMessage_ExplainsTransferOutcome()
    {
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 1));
        var storage = new InventoryState(24, 99);

        Assert.Equal(
            "Stored 1 parsnip_crop.",
            StatusMessageBuilder.BuildStorageTransferStatusMessage("parsnip_crop", changed: true, intoStorage: true, inventory, storage));

        var fullStorage = new InventoryState(1, 1);
        Assert.True(fullStorage.TryAdd("wood", 1));
        var crowdedInventory = new InventoryState(12, 99);
        Assert.True(crowdedInventory.TryAdd("stone", 1));
        Assert.Equal(
            "Cannot store stone: storage is full.",
            StatusMessageBuilder.BuildStorageTransferStatusMessage("stone", changed: false, intoStorage: true, crowdedInventory, fullStorage));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        var stockedStorage = new InventoryState(24, 99);
        Assert.True(stockedStorage.TryAdd("stone", 1));
        Assert.Equal(
            "Cannot take stone: inventory is full.",
            StatusMessageBuilder.BuildStorageTransferStatusMessage("stone", changed: false, intoStorage: false, stockedStorage, fullInventory));
    }

    [Fact]
    public void TryCompleteNextRequest_FailureMessage_UsesDisplayNamesWhenCatalogIsAvailable()
    {
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var inventory = new InventoryState(12, 99);
        var completedRequests = new HashSet<string>();
        var wallet = new Wallet(0);

        var changed = GameBootstrap.TryCompleteNextRequest(
            requests,
            new RequestBoardService(),
            inventory,
            completedRequests,
            wallet,
            CreateItemCatalog(),
            out var message);

        Assert.False(changed);
        Assert.Equal("Need 5 more Parsnip.", message);
    }

    [Fact]
    public void TryCompleteNextRequest_SuccessMessage_UsesDisplayNamesWhenCatalogIsAvailable()
    {
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        var completedRequests = new HashSet<string>();
        var wallet = new Wallet(0);

        var changed = GameBootstrap.TryCompleteNextRequest(
            requests,
            new RequestBoardService(),
            inventory,
            completedRequests,
            wallet,
            CreateItemCatalog(),
            out var message);

        Assert.True(changed);
        Assert.Equal("Completed request: delivered 5 Parsnip for 120g.", message);
    }

    [Theory]
    [InlineData(2, 0, "Click: unlock (120g)")]
    [InlineData(2, 2, "Click: unlock (120g)")]
    [InlineData(3, 3, "Click: unlock (280g)")]
    [InlineData(4, 4, "Click: unlock (600g)")]
    [InlineData(5, 5, "Click: unlock (1200g)")]
    public void GetLockedPlotHint_ReturnsTierAwareUnlockPrompt(int x, int y, string expectedHint)
    {
        var tiers = ExpansionTierService.CreateDefault();
        Assert.Equal(expectedHint, GameBootstrap.GetLockedPlotHint(x, y, tiers));
    }

    [Fact]
    public void GetLockedPlotHint_FallsBackToGenericLockedTextForOutOfTierPlots()
    {
        var tiers = ExpansionTierService.CreateDefault();
        Assert.Equal("Locked", GameBootstrap.GetLockedPlotHint(9, 9, tiers));
    }

    [Theory]
    [InlineData(2, 0, 200, true, 80, "Unlocked a new plot for 120g. Click again to till.")]
    [InlineData(3, 3, 400, true, 120, "Unlocked a new plot for 280g. Click again to till.")]
    [InlineData(4, 4, 700, true, 100, "Unlocked a new plot for 600g. Click again to till.")]
    public void TryHandleLockedPlotInteraction_UnlocksAcrossEveryTier(
        int x,
        int y,
        int startingGold,
        bool expectedChanged,
        int expectedRemainingGold,
        string expectedMessage)
    {
        var expansion = new FarmExpansionService();
        var tiers = ExpansionTierService.CreateDefault();
        var unlockState = new UnlockState(new HashSet<string>(tiers.DefaultUnlockedPlotKeys));
        var wallet = new Wallet(startingGold);

        var changed = GameBootstrap.TryHandleLockedPlotInteraction(
            expansion,
            tiers,
            unlockState,
            wallet,
            x,
            y,
            out var message);

        Assert.Equal(expectedChanged, changed);
        Assert.Equal(expectedRemainingGold, wallet.Gold);
        Assert.Equal(expectedMessage, message);
        Assert.Contains($"{x},{y}", unlockState.UnlockedPlotKeys);
    }

    [Fact]
    public void TryHandleLockedPlotInteraction_ReturnsCostMessageWhenGoldIsInsufficient()
    {
        var expansion = new FarmExpansionService();
        var tiers = ExpansionTierService.CreateDefault();
        var unlockState = new UnlockState(new HashSet<string>(tiers.DefaultUnlockedPlotKeys));
        var wallet = new Wallet(100);

        var changed = GameBootstrap.TryHandleLockedPlotInteraction(
            expansion,
            tiers,
            unlockState,
            wallet,
            x: 2,
            y: 0,
            out var message);

        Assert.False(changed);
        Assert.Equal(100, wallet.Gold);
        Assert.Equal("Need 120g to unlock this plot.", message);
        Assert.DoesNotContain("2,0", unlockState.UnlockedPlotKeys);
    }

    [Fact]
    public void TryHandleLockedPlotInteraction_ReturnsLockedMessageForPlotsOutsideAnyTier()
    {
        var expansion = new FarmExpansionService();
        var tiers = ExpansionTierService.CreateDefault();
        var unlockState = new UnlockState(new HashSet<string>(tiers.DefaultUnlockedPlotKeys));
        var wallet = new Wallet(2000);

        var changed = GameBootstrap.TryHandleLockedPlotInteraction(
            expansion,
            tiers,
            unlockState,
            wallet,
            x: 9,
            y: 9,
            out var message);

        Assert.False(changed);
        Assert.Equal(2000, wallet.Gold);
        Assert.Equal("Plot is locked.", message);
    }

    [Theory]
    [InlineData(false, false, "Fresh start. Click a plot to till, plant, water, or harvest.")]
    [InlineData(true, true, "Save loaded. Click a plot to till, plant, water, or harvest.")]
    [InlineData(true, false, "Previous save could not be read. Started a fresh day instead.")]
    public void BuildStartupFarmStatusMessage_ExplainsHowBootstrapHandledTheSave(
        bool saveFileExists,
        bool loadedExistingSave,
        string expectedMessage)
    {
        Assert.Equal(expectedMessage, StatusMessageBuilder.BuildStartupFarmStatusMessage(saveFileExists, loadedExistingSave, null, null, null, null, null));
    }

    [Fact]
    public void BuildStartupFarmStatusMessage_WhenLoadedSaveHasHarvestReadyCrops_PrioritizesHarvestFeedback()
    {
        var farmGrid = new FarmGrid(3, 3);
        farmGrid.SetPlot(new PlotState(0, 0, true, false, false, true, new CropInstance("parsnip", 4)));

        Assert.Equal(
            "Save loaded. 1 crop is ready to harvest.",
            StatusMessageBuilder.BuildStartupFarmStatusMessage(saveFileExists: true, loadedExistingSave: true, farmGrid, null, null, null, null));
    }

    [Fact]
    public void BuildStartupFarmStatusMessage_WhenLoadedSaveHasUnwateredCrops_PrioritizesWaterFeedback()
    {
        var farmGrid = new FarmGrid(3, 3);
        farmGrid.SetPlot(new PlotState(0, 0, true, false, false, false, new CropInstance("parsnip", 2)));
        farmGrid.SetPlot(new PlotState(1, 0, true, false, false, false, new CropInstance("potato", 3)));

        Assert.Equal(
            "Save loaded. 2 planted crops still need water.",
            StatusMessageBuilder.BuildStartupFarmStatusMessage(saveFileExists: true, loadedExistingSave: true, farmGrid, null, null, null, null));
    }

    [Fact]
    public void BuildStartupFarmStatusMessage_WhenLoadedSaveHasReadyRequestAndNoUrgentFarmWork_UsesRequestProgressCopy()
    {
        var farmGrid = new FarmGrid(3, 3);
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var completedRequests = new HashSet<string>();
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 5));

        Assert.Equal(
            "Save loaded. Request ready: Parsnip 5/5. Click board to turn in.",
            StatusMessageBuilder.BuildStartupFarmStatusMessage(
                saveFileExists: true,
                loadedExistingSave: true,
                farmGrid,
                requests,
                completedRequests,
                inventory,
                CreateItemCatalog()));
    }

    [Fact]
    public void BuildStartupFarmStatusMessage_WhenLoadedSaveHasCompletedAllRequestsAndNoUrgentFarmWork_UsesCompletionCopy()
    {
        var farmGrid = new FarmGrid(3, 3);
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var completedRequests = new HashSet<string> { "ship_5_parsnips" };
        var inventory = new InventoryState(12, 99);

        Assert.Equal(
            "Save loaded. All requests completed.",
            StatusMessageBuilder.BuildStartupFarmStatusMessage(
                saveFileExists: true,
                loadedExistingSave: true,
                farmGrid,
                requests,
                completedRequests,
                inventory,
                CreateItemCatalog()));
    }

    [Fact]
    public void BuildDayStartFarmStatusMessage_WhenFarmHasHarvestReadyCrops_PrioritizesHarvestFeedback()
    {
        var farmGrid = new FarmGrid(3, 3);
        farmGrid.SetPlot(new PlotState(0, 0, true, false, false, true, new CropInstance("parsnip", 4)));

        Assert.Equal(
            "A new day begins. 1 crop is ready to harvest.",
            StatusMessageBuilder.BuildDayStartFarmStatusMessage(farmGrid));
    }

    [Fact]
    public void BuildDayStartFarmStatusMessage_WhenFarmIsIdleButRequestIsReady_UsesRequestProgressCopy()
    {
        var farmGrid = new FarmGrid(3, 3);
        var requests = new[]
        {
            new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120)
        };
        var completedRequests = new HashSet<string>();
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 5));

        Assert.Equal(
            "A new day begins. Request ready: Parsnip 5/5. Click board to turn in.",
            StatusMessageBuilder.BuildDayStartFarmStatusMessage(
                farmGrid,
                requests,
                completedRequests,
                inventory,
                CreateItemCatalog()));
    }

    [Fact]
    public void BuildDayStartFarmStatusMessage_WhenFarmIsIdleAndNoRequestIsReady_UsesGenericFallback()
    {
        var farmGrid = new FarmGrid(3, 3);

        Assert.Equal(
            "A new day begins. Click a plot to till, plant, water, or harvest.",
            StatusMessageBuilder.BuildDayStartFarmStatusMessage(farmGrid));
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

    [Fact]
    public void ProcessDayEnd_WhenSeasonChanges_WithersOutOfSeasonCrops()
    {
        var springCrop = new CropDefinition("parsnip", "Parsnip", Season.Spring, "parsnip_seed", "parsnip_crop", 20, 35, 4, new[] { 1, 1, 2 });
        var summerCrop = new CropDefinition("melon", "Melon", Season.Summer, "melon_seed", "melon_crop", 80, 250, 12, new[] { 3, 4, 5 });
        var crops = new Dictionary<string, CropDefinition> { ["parsnip"] = springCrop, ["melon"] = summerCrop };
        var growth = new CropGrowthService(crops);
        var clock = new DayClock(new GameDate(Season.Spring, 28), 6 * 60, 26 * 60);
        var stamina = new StaminaState(maximum: 100, current: 100);
        var farmGrid = new FarmGrid(2, 2);

        farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant("parsnip").Water());
        farmGrid.SetPlot(PlotState.Tilled(1, 0).Plant("parsnip").Water());

        var result = GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid, crops);

        Assert.True(result.DayRolled);
        Assert.True(result.SeasonChanged);
        Assert.Equal(Season.Spring, result.PreviousSeason);
        Assert.Equal(Season.Summer, result.CurrentSeason);
        Assert.Equal(2, result.CropsWithered);
        Assert.Null(farmGrid.GetPlot(0, 0).Crop);
        Assert.Null(farmGrid.GetPlot(1, 0).Crop);
    }

    [Fact]
    public void ProcessDayEnd_WhenSeasonChanges_PreservesSameSeasonCrops()
    {
        var summerCrop = new CropDefinition("melon", "Melon", Season.Summer, "melon_seed", "melon_crop", 80, 250, 12, new[] { 3, 4, 5 });
        var crops = new Dictionary<string, CropDefinition> { ["melon"] = summerCrop };
        var growth = new CropGrowthService(crops);
        var clock = new DayClock(new GameDate(Season.Spring, 28), 6 * 60, 26 * 60);
        var stamina = new StaminaState(maximum: 100, current: 100);
        var farmGrid = new FarmGrid(2, 2);

        farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant("melon").Water());

        var result = GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid, crops);

        Assert.True(result.SeasonChanged);
        Assert.NotNull(farmGrid.GetPlot(0, 0).Crop);
        Assert.Equal("melon", farmGrid.GetPlot(0, 0).Crop!.CropId);
        Assert.Equal(1, farmGrid.GetPlot(0, 0).Crop!.DaysGrown);
        Assert.Equal(0, result.CropsWithered);
    }

    [Fact]
    public void ProcessDayEnd_WithinSameSeason_DoesNotWitherCrops()
    {
        var springCrop = new CropDefinition("parsnip", "Parsnip", Season.Spring, "parsnip_seed", "parsnip_crop", 20, 35, 4, new[] { 1, 1, 2 });
        var crops = new Dictionary<string, CropDefinition> { ["parsnip"] = springCrop };
        var growth = new CropGrowthService(crops);
        var clock = new DayClock(new GameDate(Season.Spring, 15), 6 * 60, 26 * 60);
        var stamina = new StaminaState(maximum: 100, current: 100);
        var farmGrid = new FarmGrid(2, 2);

        farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant("parsnip").Water());

        var result = GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid, crops);

        Assert.True(result.DayRolled);
        Assert.False(result.SeasonChanged);
        Assert.Equal(0, result.CropsWithered);
        Assert.NotNull(farmGrid.GetPlot(0, 0).Crop);
    }

    [Fact]
    public void BuildSeasonShopOffers_FiltersSeedsByCurrentSeason()
    {
        var springCrop = new CropDefinition("parsnip", "Parsnip", Season.Spring, "parsnip_seed", "parsnip_crop", 20, 35, 4, new[] { 1, 1, 2 });
        var summerCrop = new CropDefinition("melon", "Melon", Season.Summer, "melon_seed", "melon_crop", 80, 250, 12, new[] { 3, 4, 5 });
        var cropCatalog = new Dictionary<string, CropDefinition> { ["parsnip"] = springCrop, ["melon"] = summerCrop };

        var allOffers = new[]
        {
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            new ShopOffer("melon_seed", BuyPrice: 80, SellPrice: 40),
            new ShopOffer("parsnip_crop", BuyPrice: 0, SellPrice: 35),
            new ShopOffer("melon_crop", BuyPrice: 0, SellPrice: 250)
        };

        var springOffers = GameBootstrap.BuildSeasonShopOffers(allOffers, cropCatalog, Season.Spring);
        Assert.Contains(springOffers, o => o.ItemId == "parsnip_seed");
        Assert.DoesNotContain(springOffers, o => o.ItemId == "melon_seed");
        Assert.Contains(springOffers, o => o.ItemId == "parsnip_crop");
        Assert.Contains(springOffers, o => o.ItemId == "melon_crop");

        var summerOffers = GameBootstrap.BuildSeasonShopOffers(allOffers, cropCatalog, Season.Summer);
        Assert.DoesNotContain(summerOffers, o => o.ItemId == "parsnip_seed");
        Assert.Contains(summerOffers, o => o.ItemId == "melon_seed");
        Assert.Contains(summerOffers, o => o.ItemId == "parsnip_crop");
        Assert.Contains(summerOffers, o => o.ItemId == "melon_crop");
    }

    [Theory]
    [InlineData(GatheringHarvestOutcome.Success, "wood", "Wood", "Gathered +1 wood.")]
    [InlineData(GatheringHarvestOutcome.AlreadyHarvested, "wood", "Wood", "Wood already gathered today.")]
    [InlineData(GatheringHarvestOutcome.InventoryFull, "stone", "Stone", "Inventory full: cannot pick up stone.")]
    [InlineData(GatheringHarvestOutcome.UnknownNode, null, null, "Unknown gathering spot.")]
    public void BuildGatheringStatusMessage_FormatsEachOutcomeWithItemName(
        GatheringHarvestOutcome outcome,
        string? itemId,
        string? displayName,
        string expected)
    {
        var result = new GatheringHarvestResult(outcome, itemId);

        Assert.Equal(expected, StatusMessageBuilder.BuildGatheringStatusMessage(result, displayName));
    }

    [Fact]
    public void BuildSeasonShopOffers_KeepsMaterialItemsAvailableInEverySeason()
    {
        var springCrop = new CropDefinition("parsnip", "Parsnip", Season.Spring, "parsnip_seed", "parsnip_crop", 20, 35, 4, new[] { 1, 1, 2 });
        var cropCatalog = new Dictionary<string, CropDefinition> { ["parsnip"] = springCrop };
        var itemCatalog = new Dictionary<string, ItemDefinition>
        {
            ["wood"] = new("wood", "Wood", "Material", 99),
            ["stone"] = new("stone", "Stone", "Material", 99),
            ["parsnip_seed"] = new("parsnip_seed", "Parsnip Seeds", "Seed", 99),
        };
        var allOffers = new[]
        {
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            new ShopOffer("wood", BuyPrice: 0, SellPrice: 4),
            new ShopOffer("stone", BuyPrice: 0, SellPrice: 6),
        };

        var springOffers = GameBootstrap.BuildSeasonShopOffers(allOffers, cropCatalog, Season.Spring, itemCatalog);
        var winterOffers = GameBootstrap.BuildSeasonShopOffers(allOffers, cropCatalog, Season.Winter, itemCatalog);

        Assert.Contains(springOffers, o => o.ItemId == "wood");
        Assert.Contains(springOffers, o => o.ItemId == "stone");
        Assert.Contains(winterOffers, o => o.ItemId == "wood");
        Assert.Contains(winterOffers, o => o.ItemId == "stone");
        Assert.DoesNotContain(winterOffers, o => o.ItemId == "parsnip_seed");
    }

    [Fact]
    public void BuildDayStartFarmStatusMessage_WhenSeasonChanges_AnnouncesNewSeason()
    {
        var farmGrid = new FarmGrid(3, 3);

        Assert.Equal(
            "Summer has arrived! Click a plot to till, plant, water, or harvest.",
            StatusMessageBuilder.BuildDayStartFarmStatusMessage(farmGrid, newSeason: Season.Summer));
    }

    [Fact]
    public void BuildDayStartFarmStatusMessage_WhenSeasonChangesWithWitheredCrops_ReportsWitheredCount()
    {
        var farmGrid = new FarmGrid(3, 3);

        Assert.Equal(
            "Summer has arrived! 3 out-of-season crops have withered. Click a plot to till, plant, water, or harvest.",
            StatusMessageBuilder.BuildDayStartFarmStatusMessage(farmGrid, newSeason: Season.Summer, cropsWithered: 3));

        Assert.Equal(
            "Autumn has arrived! 1 out-of-season crop has withered. Click a plot to till, plant, water, or harvest.",
            StatusMessageBuilder.BuildDayStartFarmStatusMessage(farmGrid, newSeason: Season.Autumn, cropsWithered: 1));
    }

    [Fact]
    public void TryHandleFarmPlotInteraction_WhenOnlyOutOfSeasonSeeds_ExplainsSeasonMismatch()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(12, 99);
        inventory.TryAdd("parsnip_seed", 5);
        var farmGrid = new FarmGrid(1, 1);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var tillMessage, currentSeason: Season.Summer));
        Assert.Equal("Plot tilled. No Summer seeds available. Your seeds are for a different season.", tillMessage);

        Assert.False(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var plantMessage, currentSeason: Season.Summer));
        Assert.Equal("No Summer seeds available. Your seeds are for a different season.", plantMessage);
    }

    [Fact]
    public void TryHandleFarmPlotInteraction_WhenInSeasonSeedsExist_PlantsNormally()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(12, 99);
        inventory.TryAdd("parsnip_seed", 5);
        var farmGrid = new FarmGrid(1, 1);

        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out _, currentSeason: Season.Spring));
        Assert.True(GameBootstrap.TryHandleFarmPlotInteraction(farmGrid, inventory, crops, 0, 0, out var plantMessage, currentSeason: Season.Spring));
        Assert.Equal("Planted Parsnip. Click again to water.", plantMessage);
        Assert.Equal(4, inventory.GetQuantity("parsnip_seed"));
    }

    [Fact]
    public void BuildFarmPlotHoverStatusMessage_WhenOnlyOutOfSeasonSeeds_ExplainsSeasonMismatch()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(12, 99);
        inventory.TryAdd("parsnip_seed", 5);

        Assert.Equal(
            "Hover plot: no Summer seeds available.",
            StatusMessageBuilder.BuildFarmPlotHoverStatusMessage(
                PlotState.Tilled(0, 0), crops, inventory, currentGold: 200, currentSeason: Season.Summer));
    }

    [Fact]
    public void FindAutoPlantCrop_IgnoresOutOfSeasonCrops()
    {
        var crops = CreateCropCatalog();
        var inventory = new InventoryState(12, 99);
        inventory.TryAdd("parsnip_seed", 5);

        Assert.NotNull(GameBootstrap.FindAutoPlantCrop(crops, inventory, Season.Spring));
        Assert.Null(GameBootstrap.FindAutoPlantCrop(crops, inventory, Season.Summer));
        Assert.NotNull(GameBootstrap.FindAutoPlantCrop(crops, inventory, currentSeason: null));
    }

private static IReadOnlyDictionary<string, CropDefinition> CreateCropCatalog()
{
    return new Dictionary<string, CropDefinition>
    {
        ["parsnip"] = new(
                "parsnip",
                "Parsnip",
                Season.Spring,
                "parsnip_seed",
                "parsnip_crop",
                20,
            35,
            4,
            new[] { 1, 1, 2 })
    };
}

private static IReadOnlyDictionary<string, CropDefinition> CreateMultiCropCatalog()
{
    return new Dictionary<string, CropDefinition>(CreateCropCatalog())
    {
        ["potato"] = new(
            "potato",
            "Potato",
            Season.Spring,
            "potato_seed",
            "potato_crop",
            45,
            80,
            6,
            new[] { 2, 2, 2 })
    };
}

    private static IReadOnlyDictionary<string, ItemDefinition> CreateItemCatalog()
    {
        return new Dictionary<string, ItemDefinition>(StringComparer.Ordinal)
        {
            ["parsnip_seed"] = new("parsnip_seed", "Parsnip Seeds", "Seed", 99),
            ["parsnip_crop"] = new("parsnip_crop", "Parsnip", "Crop", 99),
            ["potato_seed"] = new("potato_seed", "Potato Seeds", "Seed", 99),
            ["potato_crop"] = new("potato_crop", "Potato", "Crop", 99),
            ["wood"] = new("wood", "Wood", "Material", 99),
            ["stone"] = new("stone", "Stone", "Material", 99)
        };
    }

    private static InventoryState CreateFullInventoryForStoneBlock()
    {
        var inventory = new InventoryState(1, 1);
        Assert.True(inventory.TryAdd("wood", 1));
        return inventory;
    }

    private static InventoryState CreateStockedStorageWithStone()
    {
        var storage = new InventoryState(24, 99);
        Assert.True(storage.TryAdd("stone", 1));
        return storage;
    }
}
