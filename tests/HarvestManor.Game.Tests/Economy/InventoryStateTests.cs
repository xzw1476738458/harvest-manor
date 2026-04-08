using HarvestManor.Core.Inventory;
using Xunit;

namespace HarvestManor.Game.Tests.Economy;

public sealed class InventoryStateTests
{
    [Fact]
    public void TryAdd_StacksIntoExistingSlotBeforeUsingNewSlot()
    {
        var inventory = new InventoryState(slotCapacity: 4, maxStackSize: 99);

        Assert.True(inventory.TryAdd("parsnip_seed", 10));
        Assert.True(inventory.TryAdd("parsnip_seed", 5));

        var stack = Assert.Single(inventory.Slots);
        Assert.Equal("parsnip_seed", stack.ItemId);
        Assert.Equal(15, stack.Quantity);
    }

    [Fact]
    public void TryAdd_ReturnsFalseWhenNotAllItemsFit()
    {
        var inventory = new InventoryState(slotCapacity: 1, maxStackSize: 10);

        var addedAll = inventory.TryAdd("wood", 11);

        Assert.False(addedAll);
        var stack = Assert.Single(inventory.Slots);
        Assert.Equal("wood", stack.ItemId);
        Assert.Equal(10, stack.Quantity);
    }

    [Fact]
    public void TryRemove_ReturnsFalseWhenInventoryHasTooFewItems()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(inventory.TryAdd("potato_seed", 3));

        var removed = inventory.TryRemove("potato_seed", 4);

        Assert.False(removed);
        Assert.Equal(3, inventory.GetQuantity("potato_seed"));
    }

    [Fact]
    public void TryRemove_RemovesAcrossMultipleStacks()
    {
        var inventory = new InventoryState(slotCapacity: 3, maxStackSize: 5);
        Assert.True(inventory.TryAdd("stone", 11));

        var removed = inventory.TryRemove("stone", 6);

        Assert.True(removed);
        Assert.Equal(5, inventory.GetQuantity("stone"));
        var stack = Assert.Single(inventory.Slots);
        Assert.Equal(5, stack.Quantity);
    }
}
