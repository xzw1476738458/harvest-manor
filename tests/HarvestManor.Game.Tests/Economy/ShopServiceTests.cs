using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;
using Xunit;

namespace HarvestManor.Game.Tests.Economy;

public sealed class ShopServiceTests
{
    [Fact]
    public void TryPurchase_RemovesGoldAndAddsItems()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        var wallet = new Wallet(200);
        var shop = new ShopService();

        var success = shop.TryPurchase(
            inventory,
            wallet,
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            3
        );

        Assert.True(success);
        Assert.Equal(140, wallet.Gold);
        Assert.Equal(3, inventory.GetQuantity("parsnip_seed"));
    }

    [Fact]
    public void TryPurchase_ReturnsFalseWhenWalletCannotCoverCost()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        var wallet = new Wallet(30);
        var shop = new ShopService();

        var success = shop.TryPurchase(
            inventory,
            wallet,
            new ShopOffer("potato_seed", BuyPrice: 20, SellPrice: 10),
            2
        );

        Assert.False(success);
        Assert.Equal(30, wallet.Gold);
        Assert.Equal(0, inventory.GetQuantity("potato_seed"));
    }

    [Fact]
    public void TryPurchase_RefundsWhenInventoryCannotFitAllItems()
    {
        var inventory = new InventoryState(slotCapacity: 1, maxStackSize: 2);
        Assert.True(inventory.TryAdd("wood", 2));
        var wallet = new Wallet(100);
        var shop = new ShopService();

        var success = shop.TryPurchase(
            inventory,
            wallet,
            new ShopOffer("parsnip_seed", BuyPrice: 20, SellPrice: 10),
            1
        );

        Assert.False(success);
        Assert.Equal(100, wallet.Gold);
        Assert.Equal(0, inventory.GetQuantity("parsnip_seed"));
    }

    [Fact]
    public void TrySell_RemovesItemsAndAddsGold()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 5));
        var wallet = new Wallet(40);
        var shop = new ShopService();

        var success = shop.TrySell(
            inventory,
            wallet,
            new ShopOffer("parsnip_crop", BuyPrice: 0, SellPrice: 35),
            3
        );

        Assert.True(success);
        Assert.Equal(145, wallet.Gold);
        Assert.Equal(2, inventory.GetQuantity("parsnip_crop"));
    }

    [Fact]
    public void TrySell_ReturnsFalseWhenInventoryIsMissingItems()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        Assert.True(inventory.TryAdd("parsnip_crop", 2));
        var wallet = new Wallet(40);
        var shop = new ShopService();

        var success = shop.TrySell(
            inventory,
            wallet,
            new ShopOffer("parsnip_crop", BuyPrice: 0, SellPrice: 35),
            3
        );

        Assert.False(success);
        Assert.Equal(40, wallet.Gold);
        Assert.Equal(2, inventory.GetQuantity("parsnip_crop"));
    }
}
