using HarvestManor.Core.Inventory;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Saves;

public sealed class SaveGameStoreTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsCoreProgress()
    {
        var snapshot = new SaveGameSnapshot(
            new GameDate(Season.Spring, 3),
            MinuteOfDay: 420,
            Gold: 180,
            Stamina: 88,
            Inventory: new List<ItemStack> { new("parsnip_seed", 8) },
            Storage: new List<ItemStack> { new("wood", 12) },
            Plots: new List<PlotSnapshot> { new(0, 0, true, false, true, "parsnip", 2) },
            UnlockedPlotKeys: new List<string> { "0,0", "1,0" },
            CompletedRequests: new List<string> { "ship_5_parsnips" }
        );

        var json = SaveGameStore.Serialize(snapshot);
        var restored = SaveGameStore.Deserialize(json);

        Assert.Equal(snapshot.Date, restored.Date);
        Assert.Equal(180, restored.Gold);
        Assert.Single(restored.Inventory);
        Assert.Single(restored.Plots);
        Assert.Single(restored.CompletedRequests);
    }
}
