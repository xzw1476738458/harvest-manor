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

        Assert.Equal(
            "Active request: parsnip_crop 0/5. Need 5 more.",
            GameBootstrap.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        Assert.Equal(
            "Request ready: parsnip_crop 5/5. Click board to turn in.",
            GameBootstrap.BuildRequestBoardStatusText(requests, completedRequests, inventory));

        Assert.True(GameBootstrap.TryTransferItem(inventory, storage, "parsnip_crop", 2));
        Assert.Equal(
            "Active request: parsnip_crop 3/5. Need 2 more.",
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

    [Theory]
    [InlineData(GameBootstrap.PanelMode.None, GameBootstrap.PanelMode.None, null)]
    [InlineData(GameBootstrap.PanelMode.None, GameBootstrap.PanelMode.Shop, "Shop open. Use Buy/Sell or press Esc to close.")]
    [InlineData(GameBootstrap.PanelMode.None, GameBootstrap.PanelMode.Storage, "Storage open. Move items or press Esc to close.")]
    [InlineData(GameBootstrap.PanelMode.Shop, GameBootstrap.PanelMode.None, null)]
    [InlineData(GameBootstrap.PanelMode.Storage, GameBootstrap.PanelMode.None, null)]
    public void BuildPanelModeStatusMessage_ExplainsPanelOpenAndCloseFlow(
        GameBootstrap.PanelMode previousMode,
        GameBootstrap.PanelMode nextMode,
        string? expectedMessage)
    {
        Assert.Equal(expectedMessage, GameBootstrap.BuildPanelModeStatusMessage(previousMode, nextMode));
    }

    [Theory]
    [InlineData(GameBootstrap.PanelMode.Shop, "Shop selection: parsnip_seed. Ready to buy 1.", "Shop selection: parsnip_seed. Ready to buy 1.")]
    [InlineData(GameBootstrap.PanelMode.Storage, "Stored 1 parsnip_seed. Storage selection: store parsnip_seed or take wood.", "Stored 1 parsnip_seed. Storage selection: store parsnip_seed or take wood.")]
    [InlineData(GameBootstrap.PanelMode.Shop, null, "Panels closed. Interact with the world again.")]
    public void BuildPanelCloseStatusMessage_RestoresTheLatestPanelContextWhenAvailable(
        GameBootstrap.PanelMode previousMode,
        string? latestPanelContextMessage,
        string expectedMessage)
    {
        Assert.Equal(expectedMessage, GameBootstrap.BuildPanelCloseStatusMessage(previousMode, latestPanelContextMessage));
    }

    [Fact]
    public void BuildFarmPlotHoverStatusMessage_ExplainsTheCurrentPlotActionBeforeClick()
    {
        var crops = CreateCropCatalog();

        Assert.Equal(
            "Hover plot (2,0): unlock for 120g.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(new PlotState(2, 0, false, true, false, false, null), crops));
        Assert.Equal(
            "Hover plot (0,0): click to till.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(PlotState.Wild(0, 0), crops));
        Assert.Equal(
            "Hover plot (0,0): click to plant.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(PlotState.Tilled(0, 0), crops));
        Assert.Equal(
            "Hover Parsnip: click to water.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, false, false, new CropInstance("parsnip", 2)),
                crops));
        Assert.Equal(
            "Hover Parsnip: watered today.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, true, false, new CropInstance("parsnip", 2)),
                crops));
        Assert.Equal(
            "Hover Parsnip: ready to harvest.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, true, true, new CropInstance("parsnip", 4)),
                crops));
    }

    [Fact]
    public void BuildFarmPlotHoverStatusMessage_ReflectsCurrentResourcesBeforeClick()
    {
        var crops = CreateCropCatalog();

        var emptyInventory = new InventoryState(12, 99);
        Assert.Equal(
            "Hover plot (0,0): no seeds available.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(
                PlotState.Tilled(0, 0),
                crops,
                emptyInventory,
                currentGold: 200));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        Assert.Equal(
            "Hover Parsnip: inventory full.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(
                new PlotState(0, 0, true, false, true, true, new CropInstance("parsnip", 4)),
                crops,
                fullInventory,
                currentGold: 200));

        Assert.Equal(
            "Hover plot (2,0): need 120g to unlock.",
            GameBootstrap.BuildFarmPlotHoverStatusMessage(
                new PlotState(2, 0, false, true, false, false, null),
                crops,
                emptyInventory,
                currentGold: 100));
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
        Assert.Equal(expectedMessage, GameBootstrap.BuildInteractionHoverStatusMessage(interactionName, actionDescription));
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
            "Hover request board: need 5 more parsnip_crop.",
            GameBootstrap.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));

        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        Assert.Equal(
            "Hover request board: turn in 5 parsnip_crop for 120g.",
            GameBootstrap.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));

        completedRequests.Add("ship_5_parsnips");
        Assert.Equal(
            "Hover request board: all requests completed.",
            GameBootstrap.BuildRequestBoardHoverStatusMessage(requests, completedRequests, inventory));
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
            GameBootstrap.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, inventory, wallet));

        Assert.True(inventory.TryAdd("parsnip_crop", 1));
        Assert.Equal(
            "Shop selection: parsnip_crop. Ready to sell 1.",
            GameBootstrap.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 1, inventory, wallet));

        var ownedSeeds = new InventoryState(1, 1);
        Assert.True(ownedSeeds.TryAdd("parsnip_seed", 1));
        Assert.Equal(
            "Shop selection: parsnip_seed. Ready to sell 1. Cannot buy 1: inventory full.",
            GameBootstrap.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, ownedSeeds, wallet));

        var poorWallet = new Wallet(5);
        var seedInventory = new InventoryState(12, 99);
        Assert.True(seedInventory.TryAdd("parsnip_seed", 1));
        Assert.Equal(
            "Shop selection: parsnip_seed. Ready to sell 1. Need 15g more to buy 1.",
            GameBootstrap.BuildShopBrowseStatusMessage(offers, selectedOfferIndex: 0, seedInventory, poorWallet));
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
            GameBootstrap.BuildStorageBrowseStatusMessage(inventory, storage));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("stone", 1));
        var stockedStorage = new InventoryState(24, 99);
        Assert.True(stockedStorage.TryAdd("wood", 1));
        Assert.Equal(
            "Storage selection: store stone. Cannot take wood: inventory is full.",
            GameBootstrap.BuildStorageBrowseStatusMessage(fullInventory, stockedStorage));

        var blockedInventory = new InventoryState(1, 1);
        Assert.True(blockedInventory.TryAdd("stone", 1));
        var blockedStorage = new InventoryState(1, 1);
        Assert.True(blockedStorage.TryAdd("wood", 1));
        Assert.Equal(
            "Storage blocked: cannot store stone (storage full) or take wood (inventory full).",
            GameBootstrap.BuildStorageBrowseStatusMessage(blockedInventory, blockedStorage));
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
            GameBootstrap.BuildShopActionStatusMessage(
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
            GameBootstrap.BuildStorageActionStatusMessage(
                "Stored 1 parsnip_seed.",
                inventory,
                storage));
    }

    [Fact]
    public void BuildShopPurchaseStatusMessage_ExplainsPurchaseOutcome()
    {
        var offer = new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10);
        var wallet = new Wallet(200);
        var inventory = new InventoryState(12, 99);

        Assert.Equal(
            "Bought 1 parsnip_seed for 20g.",
            GameBootstrap.BuildShopPurchaseStatusMessage(offer, inventory, wallet, changed: true));

        var poorWallet = new Wallet(5);
        Assert.Equal(
            "Need 15g more to buy 1 parsnip_seed.",
            GameBootstrap.BuildShopPurchaseStatusMessage(offer, inventory, poorWallet, changed: false));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        Assert.Equal(
            "Cannot buy parsnip_seed: inventory full.",
            GameBootstrap.BuildShopPurchaseStatusMessage(offer, fullInventory, wallet, changed: false));
    }

    [Fact]
    public void BuildShopSellStatusMessage_ExplainsSellOutcome()
    {
        var offer = new ShopOffer("parsnip_crop", BuyPrice: 20, SellPrice: 35);
        var stockedInventory = new InventoryState(12, 99);
        Assert.True(stockedInventory.TryAdd("parsnip_crop", 1));

        Assert.Equal(
            "Sold 1 parsnip_crop for 35g.",
            GameBootstrap.BuildShopSellStatusMessage(offer, stockedInventory, changed: true));

        var emptyInventory = new InventoryState(12, 99);
        Assert.Equal(
            "Cannot sell parsnip_crop: none available.",
            GameBootstrap.BuildShopSellStatusMessage(offer, emptyInventory, changed: false));
    }

    [Fact]
    public void BuildStorageTransferStatusMessage_ExplainsTransferOutcome()
    {
        var inventory = new InventoryState(12, 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 1));
        var storage = new InventoryState(24, 99);

        Assert.Equal(
            "Stored 1 parsnip_crop.",
            GameBootstrap.BuildStorageTransferStatusMessage("parsnip_crop", changed: true, intoStorage: true, inventory, storage));

        var fullStorage = new InventoryState(1, 1);
        Assert.True(fullStorage.TryAdd("wood", 1));
        var crowdedInventory = new InventoryState(12, 99);
        Assert.True(crowdedInventory.TryAdd("stone", 1));
        Assert.Equal(
            "Cannot store stone: storage is full.",
            GameBootstrap.BuildStorageTransferStatusMessage("stone", changed: false, intoStorage: true, crowdedInventory, fullStorage));

        var fullInventory = new InventoryState(1, 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        var stockedStorage = new InventoryState(24, 99);
        Assert.True(stockedStorage.TryAdd("stone", 1));
        Assert.Equal(
            "Cannot take stone: inventory is full.",
            GameBootstrap.BuildStorageTransferStatusMessage("stone", changed: false, intoStorage: false, stockedStorage, fullInventory));
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
