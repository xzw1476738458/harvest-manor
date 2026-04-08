using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;
using HarvestManor.UI;
using Xunit;

namespace HarvestManor.Game.Tests.UI;

public sealed class PanelControllerStateTests
{
    [Fact]
    public void EvaluateOfferState_DisablesBuyWhenInventoryCannotFitSelectedOffer()
    {
        var inventory = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        Assert.True(inventory.TryAdd("wood", 1));
        var wallet = new Wallet(200);

        var state = ShopPanelController.EvaluateOfferState(
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            inventory,
            wallet);

        Assert.False(state.CanBuy);
        Assert.Equal("Inventory full for selected offer.", state.StatusText);
    }

    [Fact]
    public void EvaluateOfferState_ReportsMissingGoldWhenWalletCannotCoverCost()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        var wallet = new Wallet(5);

        var state = ShopPanelController.EvaluateOfferState(
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            inventory,
            wallet);

        Assert.False(state.CanBuy);
        Assert.Equal("Need 15g more to buy 1.", state.StatusText);
    }

    [Fact]
    public void EvaluateTransferState_DisablesWithdrawWhenInventoryCannotFitSelectedItem()
    {
        var inventory = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        Assert.True(inventory.TryAdd("wood", 1));

        var storage = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(storage.TryAdd("stone", 1));

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);

        Assert.False(state.CanWithdraw);
        Assert.Equal("Inventory is full for the selected item.", state.StatusText);
    }
}
