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

        if (!inventory.CanAdd(offer.ItemId, quantity))
        {
            return false;
        }

        var snapshot = inventory.CreateSnapshot();
        var totalCostLong = (long)offer.BuyPrice * quantity;
        if (totalCostLong > int.MaxValue || !wallet.TrySpend((int)totalCostLong))
        {
            return false;
        }

        if (inventory.TryAdd(offer.ItemId, quantity))
        {
            return true;
        }

        inventory.RestoreSnapshot(snapshot);
        wallet.Earn((int)totalCostLong);
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

        var totalRevenueLong = (long)offer.SellPrice * quantity;
        wallet.Earn(totalRevenueLong > int.MaxValue ? int.MaxValue : (int)totalRevenueLong);
        return true;
    }
}
