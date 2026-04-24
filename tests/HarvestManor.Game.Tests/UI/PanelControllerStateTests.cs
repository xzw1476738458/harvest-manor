using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;
using HarvestManor.UI;
using Xunit;

namespace HarvestManor.Game.Tests.UI;

public sealed class PanelControllerStateTests
{
    [Fact]
    public void BuildInventoryBodyText_UsesDisplayNamesWhenCatalogIsAvailable()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 2));
        Assert.True(inventory.TryAdd("wood", 1));

        var bodyText = InventoryPanelController.BuildBodyText(inventory, CreateItemCatalog());

        Assert.Contains("Parsnip Seeds [color=#c8a864]x2[/color]", bodyText);
        Assert.Contains("Wood [color=#c8a864]x1[/color]", bodyText);
        Assert.DoesNotContain("parsnip_seed", bodyText);
    }

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
    public void EvaluateOfferState_ReportsSellReadinessWhenBuyIsBlockedByInventorySpace()
    {
        var inventory = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        var wallet = new Wallet(200);

        var state = ShopPanelController.EvaluateOfferState(
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            inventory,
            wallet);

        Assert.False(state.CanBuy);
        Assert.True(state.CanSell);
        Assert.Equal("Ready to sell 1. Cannot buy 1: inventory full.", state.StatusText);
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
    public void EvaluateOfferState_ReportsSellReadinessWhenBuyIsBlockedByMissingGold()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        var wallet = new Wallet(5);

        var state = ShopPanelController.EvaluateOfferState(
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            inventory,
            wallet);

        Assert.False(state.CanBuy);
        Assert.True(state.CanSell);
        Assert.Equal("Ready to sell 1. Need 15g more to buy 1.", state.StatusText);
    }

    [Fact]
    public void BuildBuyButtonText_ExplainsWhyBuyingIsDisabled()
    {
        var fullInventory = new InventoryState(slotCapacity: 1, maxStackSize: 1);
        Assert.True(fullInventory.TryAdd("wood", 1));
        var richWallet = new Wallet(200);

        Assert.Equal(
            "Inventory full",
            ShopPanelController.BuildBuyButtonText(
                new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
                fullInventory,
                richWallet));

        var roomyInventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        var poorWallet = new Wallet(5);

        Assert.Equal(
            "Need 15g more",
            ShopPanelController.BuildBuyButtonText(
                new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
                roomyInventory,
                poorWallet));
    }

    [Fact]
    public void BuildSellButtonText_ExplainsWhySellingIsDisabled()
    {
        var emptyInventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        var wallet = new Wallet(200);

        Assert.Equal(
            "Nothing to sell",
            ShopPanelController.BuildSellButtonText(
                new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
                emptyInventory,
                wallet));

        Assert.Equal(
            "Cannot sell here",
            ShopPanelController.BuildSellButtonText(
                new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 0),
                emptyInventory,
                wallet));
    }

    [Fact]
    public void BuildShopBodyText_UsesDisplayNamesWhenCatalogIsAvailable()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 1));
        var wallet = new Wallet(200);

        var bodyText = ShopPanelController.BuildBodyText(
            new[] { new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10) },
            selectedOfferIndex: 0,
            inventory,
            wallet,
            CreateItemCatalog());

        Assert.Contains("Item:[/b] Parsnip Seeds", bodyText);
        Assert.DoesNotContain("parsnip_seed", bodyText);
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
    public void StoragePanelSurfaceHelpers_UseDisplayNamesWhenCatalogIsAvailable()
    {
        var inventory = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(inventory.TryAdd("parsnip_seed", 2));

        var storage = new InventoryState(slotCapacity: 2, maxStackSize: 99);
        Assert.True(storage.TryAdd("potato_crop", 1));

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);
        var itemCatalog = CreateItemCatalog();
        var bodyText = StoragePanelController.BuildBodyText(inventory, storage, itemCatalog);

        Assert.Contains("Parsnip Seeds [color=#c8a864]x2[/color]", bodyText);
        Assert.Contains("Potato [color=#c8a864]x1[/color]", bodyText);
        Assert.Equal("Store 1 Parsnip Seeds", StoragePanelController.BuildStoreButtonText(state, itemCatalog));
        Assert.Equal("Take 1 Potato", StoragePanelController.BuildWithdrawButtonText(state, itemCatalog));
        Assert.DoesNotContain("parsnip_seed", bodyText);
        Assert.DoesNotContain("potato_crop", bodyText);
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

    private static IReadOnlyDictionary<string, ItemDefinition> CreateItemCatalog()
    {
        return new Dictionary<string, ItemDefinition>(StringComparer.Ordinal)
        {
            ["parsnip_seed"] = new("parsnip_seed", "Parsnip Seeds", "Seed", 99),
            ["potato_crop"] = new("potato_crop", "Potato", "Crop", 99),
            ["wood"] = new("wood", "Wood", "Material", 99),
            ["stone"] = new("stone", "Stone", "Material", 99)
        };
    }
}
