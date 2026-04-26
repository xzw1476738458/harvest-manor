using HarvestManor.Core.Gathering;
using HarvestManor.Core.Inventory;
using Xunit;

namespace HarvestManor.Game.Tests.Gathering;

public sealed class GatheringServiceTests
{
    [Fact]
    public void TryHarvest_AddsItemToInventoryAndMarksNodeHarvested()
    {
        var (service, state, inventory) = BuildService();

        var result = service.TryHarvest("forest_tree_1", inventory);

        Assert.Equal(GatheringHarvestOutcome.Success, result.Outcome);
        Assert.Equal("wood", result.ItemId);
        Assert.Equal(1, inventory.GetQuantity("wood"));
        Assert.True(state.IsHarvested("forest_tree_1"));
    }

    [Fact]
    public void TryHarvest_ReturnsAlreadyHarvestedAndDoesNotChangeInventory()
    {
        var (service, state, inventory) = BuildService();
        service.TryHarvest("forest_tree_1", inventory);

        var second = service.TryHarvest("forest_tree_1", inventory);

        Assert.Equal(GatheringHarvestOutcome.AlreadyHarvested, second.Outcome);
        Assert.Equal("wood", second.ItemId);
        Assert.Equal(1, inventory.GetQuantity("wood"));
        Assert.True(state.IsHarvested("forest_tree_1"));
    }

    [Fact]
    public void TryHarvest_ReturnsUnknownNodeWhenNodeIdIsNotRegistered()
    {
        var (service, _, inventory) = BuildService();

        var result = service.TryHarvest("ghost_node", inventory);

        Assert.Equal(GatheringHarvestOutcome.UnknownNode, result.Outcome);
        Assert.Null(result.ItemId);
        Assert.Equal(0, inventory.GetQuantity("wood"));
    }

    [Fact]
    public void TryHarvest_ReturnsInventoryFullWhenAddIsRefused()
    {
        var (service, state, _) = BuildService();
        var tinyInventory = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        tinyInventory.TryAdd("parsnip_seed", 1);

        var result = service.TryHarvest("forest_tree_1", tinyInventory);

        Assert.Equal(GatheringHarvestOutcome.InventoryFull, result.Outcome);
        Assert.Equal("wood", result.ItemId);
        Assert.False(state.IsHarvested("forest_tree_1"));
        Assert.Equal(0, tinyInventory.GetQuantity("wood"));
    }

    [Fact]
    public void ResetForNewDay_RestoresAllNodesSoTheyCanBeHarvestedAgain()
    {
        var (service, _, inventory) = BuildService();
        service.TryHarvest("forest_tree_1", inventory);
        service.TryHarvest("quarry_rock_1", inventory);

        service.ResetForNewDay();

        Assert.Equal(GatheringHarvestOutcome.Success, service.TryHarvest("forest_tree_1", inventory).Outcome);
        Assert.Equal(GatheringHarvestOutcome.Success, service.TryHarvest("quarry_rock_1", inventory).Outcome);
        Assert.Equal(2, inventory.GetQuantity("wood"));
        Assert.Equal(2, inventory.GetQuantity("stone"));
    }

    [Fact]
    public void Nodes_ExposesEveryRegisteredNodeKeyedById()
    {
        var (service, _, _) = BuildService();

        Assert.Equal(3, service.Nodes.Count);
        Assert.True(service.Nodes.ContainsKey("forest_tree_1"));
        Assert.True(service.Nodes.ContainsKey("forest_tree_2"));
        Assert.True(service.Nodes.ContainsKey("quarry_rock_1"));
    }

    [Fact]
    public void Constructor_RejectsDuplicateNodeIds()
    {
        var nodes = new[]
        {
            new GatheringNodeDefinition("forest_tree_1", "wood"),
            new GatheringNodeDefinition("forest_tree_1", "stone")
        };

        Assert.Throws<ArgumentException>(() => new GatheringService(nodes, new GatheringState()));
    }

    private static (GatheringService Service, GatheringState State, InventoryState Inventory) BuildService()
    {
        var state = new GatheringState();
        var nodes = new[]
        {
            new GatheringNodeDefinition("forest_tree_1", "wood"),
            new GatheringNodeDefinition("forest_tree_2", "wood"),
            new GatheringNodeDefinition("quarry_rock_1", "stone")
        };
        var service = new GatheringService(nodes, state);
        var inventory = new InventoryState(slotCapacity: 12, maxStackSize: 99);
        return (service, state, inventory);
    }
}
