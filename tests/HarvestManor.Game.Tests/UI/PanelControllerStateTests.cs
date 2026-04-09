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
        Assert.Equal("Ready to store 1 wood. Cannot take stone: inventory is full.", state.StatusText);
    }

    [Fact]
    public void EvaluateTransferState_ReportsBothBlockedDirectionsWithItemNames()
    {
        var inventory = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        Assert.True(inventory.TryAdd("stone", 1));

        var storage = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        Assert.True(storage.TryAdd("wood", 1));

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);

        Assert.False(state.CanStore);
        Assert.False(state.CanWithdraw);
        Assert.Equal("Cannot store stone: storage is full. Cannot take wood: inventory is full.", state.StatusText);
    }

    [Fact]
    public void BuildTransferButtonText_ExplainsBlockedDirections()
    {
        var blockedState = new StoragePanelController.TransferUiState(
            StoreCandidateItemId: "stone",
            WithdrawCandidateItemId: "wood",
            CanStore: false,
            CanWithdraw: false,
            StatusText: "Cannot store stone: storage is full. Cannot take wood: inventory is full.");

        Assert.Equal("Storage full for stone", StoragePanelController.BuildStoreButtonText(blockedState));
        Assert.Equal("Inventory full for wood", StoragePanelController.BuildWithdrawButtonText(blockedState));
    }

    [Fact]
    public void EvaluateTransferState_PicksFirstStoreCandidateThatActuallyFits()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 2);
        Assert.True(inventory.TryAdd("wood", 1));
        Assert.True(inventory.TryAdd("parsnip_seed", 1));

        var storage = new InventoryState(slotCapacity: 2, maxStackSize: 2);
        Assert.True(storage.TryAdd("wood", 2));
        Assert.True(storage.TryAdd("parsnip_seed", 1));

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);

        Assert.True(state.CanStore);
        Assert.Equal("parsnip_seed", state.StoreCandidateItemId);
        Assert.Equal("Use Store or Take to move 1 item.", state.StatusText);
    }

    [Fact]
    public void EvaluateTransferState_PicksFirstWithdrawCandidateThatActuallyFits()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 2);
        Assert.True(inventory.TryAdd("wood", 2));
        Assert.True(inventory.TryAdd("parsnip_seed", 1));

        var storage = new InventoryState(slotCapacity: 2, maxStackSize: 2);
        Assert.True(storage.TryAdd("wood", 1));
        Assert.True(storage.TryAdd("parsnip_seed", 1));

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);

        Assert.True(state.CanWithdraw);
        Assert.Equal("parsnip_seed", state.WithdrawCandidateItemId);
        Assert.Equal("Use Store or Take to move 1 item.", state.StatusText);
    }
}
