using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using HarvestManor.World;
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
