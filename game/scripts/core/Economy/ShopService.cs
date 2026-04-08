using HarvestManor.Core.Inventory;

namespace HarvestManor.Core.Economy;

public sealed class ShopService
{
    public bool TryPurchase(InventoryState inventory, Wallet wallet, ShopOffer offer, int quantity)
    {
        if (quantity <= 0 || offer.BuyPrice < 0)
        {
            return false;
        }

        var totalCost = offer.BuyPrice * quantity;
        if (!wallet.TrySpend(totalCost))
        {
            return false;
        }

        var beforeQuantity = inventory.GetQuantity(offer.ItemId);
        if (inventory.TryAdd(offer.ItemId, quantity))
        {
            return true;
        }

        var addedQuantity = inventory.GetQuantity(offer.ItemId) - beforeQuantity;
        if (addedQuantity > 0)
        {
            _ = inventory.TryRemove(offer.ItemId, addedQuantity);
        }

        wallet.Earn(totalCost);
        return false;
    }

    public bool TrySell(InventoryState inventory, Wallet wallet, ShopOffer offer, int quantity)
    {
        if (quantity <= 0 || offer.SellPrice < 0)
        {
            return false;
        }

        if (!inventory.TryRemove(offer.ItemId, quantity))
        {
            return false;
        }

        wallet.Earn(offer.SellPrice * quantity);
        return true;
    }
}
