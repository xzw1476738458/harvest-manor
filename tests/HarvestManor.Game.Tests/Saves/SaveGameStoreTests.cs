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
            Plots: new List<PlotSnapshot> { new(0, 0, true, false, true, true, "parsnip", 2) },
            UnlockedPlotKeys: new List<string> { "0,0", "1,0" },
            CompletedRequests: new List<string> { "ship_5_parsnips" },
            HarvestedGatheringNodeIds: new List<string> { "forest_tree_1", "quarry_rock_2" }
        );

        var json = SaveGameStore.Serialize(snapshot);
        var restored = SaveGameStore.Deserialize(json);

        Assert.Equal(snapshot.Date, restored.Date);
        Assert.Equal(180, restored.Gold);
        Assert.Single(restored.Inventory);
        var plot = Assert.Single(restored.Plots);
        Assert.True(plot.IsWateredToday);
        Assert.Single(restored.CompletedRequests);
        Assert.Equal(2, restored.HarvestedGatheringNodeIds.Count);
        Assert.Contains("forest_tree_1", restored.HarvestedGatheringNodeIds);
        Assert.Contains("quarry_rock_2", restored.HarvestedGatheringNodeIds);
    }

    [Fact]
    public void Deserialize_ThrowsWhenRequiredCollectionsAreMissing()
    {
        var incompleteJson =
            """
            {
              "date": { "season": "Spring", "day": 3 },
              "minuteOfDay": 420,
              "gold": 180,
              "stamina": 88
            }
            """;

        Assert.Throws<InvalidDataException>(() => SaveGameStore.Deserialize(incompleteJson));
    }

    [Fact]
    public void Deserialize_ThrowsWhenDateDayIsInvalid()
    {
        var invalidJson =
            """
            {
              "date": { "season": "Spring", "day": 0 },
              "minuteOfDay": 420,
              "gold": 180,
              "stamina": 88,
              "inventory": [],
              "storage": [],
              "plots": [],
              "unlockedPlotKeys": [],
              "completedRequests": []
            }
            """;

        Assert.Throws<InvalidDataException>(() => SaveGameStore.Deserialize(invalidJson));
    }

    [Fact]
    public void Deserialize_LoadsLegacyPayloadsThatOmitLaterProgressCollections()
    {
        var legacyJson =
            """
            {
              "date": { "season": 0, "day": 4 },
              "minuteOfDay": 480,
              "gold": 250,
              "stamina": 90,
              "inventory": [
                { "itemId": "parsnip_seed", "quantity": 3 }
              ],
              "storage": [
                { "itemId": "wood", "quantity": 12 }
              ],
              "plots": [
                {
                  "x": 0,
                  "y": 0,
                  "isTilled": true,
                  "isLocked": false,
                  "isHarvestReady": false,
                  "cropId": "parsnip",
                  "daysGrown": 2
                }
              ]
            }
            """;

        var snapshot = SaveGameStore.Deserialize(legacyJson);

        Assert.Equal(new GameDate(Season.Spring, 4), snapshot.Date);
        Assert.Single(snapshot.Inventory);
        Assert.Single(snapshot.Storage);
        var plot = Assert.Single(snapshot.Plots);
        Assert.False(plot.IsWateredToday);
        Assert.Empty(snapshot.UnlockedPlotKeys);
        Assert.Empty(snapshot.CompletedRequests);
        Assert.Empty(snapshot.HarvestedGatheringNodeIds);
    }
}
