using HarvestManor.Core.Inventory;

namespace HarvestManor.Core.Progression;

public sealed class RequestBoardService
{
    public bool TryComplete(RequestDefinition request, InventoryState inventory, out int rewardGold)
    {
        rewardGold = 0;

        if (!inventory.TryRemove(request.RequiredItemId, request.RequiredQuantity))
        {
            return false;
        }

        rewardGold = request.RewardGold;
        return true;
    }
}
