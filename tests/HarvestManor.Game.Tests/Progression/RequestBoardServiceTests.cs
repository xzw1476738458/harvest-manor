using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class RequestBoardServiceTests
{
    [Fact]
    public void CompleteRequest_RemovesRequiredItemsAndMarksRequestDone()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        inventory.TryAdd("parsnip_crop", 5);

        var service = new RequestBoardService();
        var request = new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120);

        var completed = service.TryComplete(request, inventory, out var reward);

        Assert.True(completed);
        Assert.Equal(120, reward);
        Assert.Equal(0, inventory.GetQuantity("parsnip_crop"));
    }
}
