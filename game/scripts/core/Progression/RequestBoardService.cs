using HarvestManor.Core.Inventory;

namespace HarvestManor.Core.Progression;

public sealed class RequestBoardService
{
    public bool TryComplete(
        RequestDefinition request,
        InventoryState inventory,
        ISet<string> completedRequestIds,
        out int rewardGold)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(completedRequestIds);

        rewardGold = 0;

        if (completedRequestIds.Contains(request.Id))
        {
            return false;
        }

        if (!inventory.TryRemove(request.RequiredItemId, request.RequiredQuantity))
        {
            return false;
        }

        completedRequestIds.Add(request.Id);
        rewardGold = request.RewardGold;
        return true;
    }
}
