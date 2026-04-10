using System.Linq;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    public static bool ProcessDayEnd(
        DayClock clock,
        StaminaState stamina,
        CropGrowthService growth,
        FarmGrid farmGrid,
        int minutesToAdvance = 20 * 60)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(stamina);
        ArgumentNullException.ThrowIfNull(growth);
        ArgumentNullException.ThrowIfNull(farmGrid);

        var rolled = clock.AdvanceMinutes(minutesToAdvance);
        if (!rolled)
        {
            return false;
        }

        foreach (var plot in farmGrid.AllPlots.ToList())
        {
            farmGrid.SetPlot(growth.AdvanceDay(plot));
        }

        stamina.RestoreFull();
        return rolled;
    }

    public static bool TryApplyShopOpenSideEffects(
        InventoryState inventory,
        Wallet wallet,
        IReadOnlyList<ShopOffer> offers)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(wallet);
        ArgumentNullException.ThrowIfNull(offers);
        return false;
    }

    public static string GetLockedPlotHint(int x, int y)
    {
        return BuildPlotKey(x, y) == DemoExpansionPlotKey
            ? $"Click: unlock ({DemoExpansionCost}g)"
            : "Locked";
    }

    public static bool TryHandleLockedPlotInteraction(
        FarmExpansionService expansionService,
        UnlockState unlockState,
        int currentGold,
        int x,
        int y,
        out int updatedGold,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(expansionService);
        ArgumentNullException.ThrowIfNull(unlockState);

        var plotKey = BuildPlotKey(x, y);
        if (plotKey != DemoExpansionPlotKey)
        {
            updatedGold = currentGold;
            message = "Plot is locked.";
            return false;
        }

        if (expansionService.TryUnlockPlot(unlockState, plotKey, DemoExpansionCost, currentGold, out updatedGold))
        {
            message = $"Unlocked a new plot for {DemoExpansionCost}g. Click again to till.";
            return true;
        }

        message = currentGold < DemoExpansionCost
            ? $"Need {DemoExpansionCost}g to unlock this plot."
            : "Plot is locked.";
        return false;
    }

    public static string BuildStorageBrowseStatusMessage(
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(storage);

        var state = StoragePanelController.EvaluateTransferState(inventory, storage);
        var storeDisplayName = state.StoreCandidateItemId is null
            ? null
            : ItemDisplayNameFormatter.Resolve(state.StoreCandidateItemId, itemCatalog);
        var withdrawDisplayName = state.WithdrawCandidateItemId is null
            ? null
            : ItemDisplayNameFormatter.Resolve(state.WithdrawCandidateItemId, itemCatalog);

        if (state.StoreCandidateItemId is null && state.WithdrawCandidateItemId is null)
        {
            return "Storage open. Nothing to move.";
        }

        if (state.CanStore && state.CanWithdraw &&
            state.StoreCandidateItemId is not null &&
            state.WithdrawCandidateItemId is not null)
        {
            return $"Storage selection: store {storeDisplayName} or take {withdrawDisplayName}.";
        }

        if (state.CanStore && state.StoreCandidateItemId is not null)
        {
            return state.WithdrawCandidateItemId is not null
                ? $"Storage selection: store {storeDisplayName}. Cannot take {withdrawDisplayName}: inventory is full."
                : $"Storage selection: store {storeDisplayName}.";
        }

        if (state.CanWithdraw && state.WithdrawCandidateItemId is not null)
        {
            return state.StoreCandidateItemId is not null
                ? $"Storage selection: take {withdrawDisplayName}. Cannot store {storeDisplayName}: storage is full."
                : $"Storage selection: take {withdrawDisplayName}.";
        }

        if (state.StoreCandidateItemId is not null && state.WithdrawCandidateItemId is not null)
        {
            return $"Storage blocked: cannot store {storeDisplayName} (storage full) or take {withdrawDisplayName} (inventory full).";
        }

        if (state.StoreCandidateItemId is not null)
        {
            return $"Storage blocked: cannot store {storeDisplayName} (storage full).";
        }

        if (state.WithdrawCandidateItemId is not null)
        {
            return $"Storage blocked: cannot take {withdrawDisplayName} (inventory full).";
        }

        return "Storage open. Nothing to move.";
    }

    public static string BuildShopActionStatusMessage(
        string actionMessage,
        IReadOnlyList<ShopOffer> offers,
        int selectedOfferIndex,
        InventoryState inventory,
        Wallet wallet,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionMessage);
        return $"{actionMessage} {BuildShopBrowseStatusMessage(offers, selectedOfferIndex, inventory, wallet, itemCatalog)}";
    }

    public static string BuildStorageActionStatusMessage(
        string actionMessage,
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionMessage);
        return $"{actionMessage} {BuildStorageBrowseStatusMessage(inventory, storage, itemCatalog)}";
    }

    public static string BuildRequestBoardActionStatusMessage(
        string actionMessage,
        IReadOnlyList<RequestDefinition> requests,
        ISet<string> completedRequestIds,
        InventoryState inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionMessage);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(inventory);

        var currentStatus = BuildRequestBoardStatusText(requests, completedRequestIds, inventory, itemCatalog);
        if (!actionMessage.StartsWith("Completed request", StringComparison.Ordinal))
        {
            return currentStatus;
        }

        return $"{actionMessage} {currentStatus}";
    }

    public static string BuildShopPurchaseStatusMessage(
        ShopOffer offer,
        InventoryState inventory,
        Wallet wallet,
        bool changed,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(wallet);

        var itemDisplayName = ItemDisplayNameFormatter.Resolve(offer.ItemId, itemCatalog);

        if (changed)
        {
            return $"Bought 1 {itemDisplayName} for {offer.BuyPrice}g.";
        }

        if (!inventory.CanAdd(offer.ItemId, 1))
        {
            return $"Cannot buy {itemDisplayName}: inventory full.";
        }

        var missingGold = Math.Max(0, offer.BuyPrice - wallet.Gold);
        if (missingGold > 0)
        {
            return $"Need {missingGold}g more to buy 1 {itemDisplayName}.";
        }

        return $"Cannot buy {itemDisplayName}.";
    }

    public static string BuildShopSellStatusMessage(
        ShopOffer offer,
        InventoryState inventory,
        bool changed,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(inventory);

        var itemDisplayName = ItemDisplayNameFormatter.Resolve(offer.ItemId, itemCatalog);

        if (changed)
        {
            return $"Sold 1 {itemDisplayName} for {offer.SellPrice}g.";
        }

        return inventory.GetQuantity(offer.ItemId) > 0
            ? $"Cannot sell {itemDisplayName}."
            : $"Cannot sell {itemDisplayName}: none available.";
    }

    public static string BuildStorageTransferStatusMessage(
        string itemId,
        bool changed,
        bool intoStorage,
        InventoryState source,
        InventoryState destination,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var itemDisplayName = ItemDisplayNameFormatter.Resolve(itemId, itemCatalog);

        if (changed)
        {
            return intoStorage
                ? $"Stored 1 {itemDisplayName}."
                : $"Took 1 {itemDisplayName} from storage.";
        }

        if (source.GetQuantity(itemId) <= 0)
        {
            return intoStorage
                ? $"Cannot store {itemDisplayName}: none available."
                : $"Cannot take {itemDisplayName}: none available.";
        }

        if (!destination.CanAdd(itemId, 1))
        {
            return intoStorage
                ? $"Cannot store {itemDisplayName}: storage is full."
                : $"Cannot take {itemDisplayName}: inventory is full.";
        }

        return intoStorage
            ? $"Cannot store {itemDisplayName}."
            : $"Cannot take {itemDisplayName}.";
    }

    public static string BuildRequestBoardStatusText(
        IReadOnlyList<RequestDefinition> requests,
        ISet<string> completedRequestIds,
        InventoryState inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(inventory);

        var nextRequest = requests.FirstOrDefault(request => !completedRequestIds.Contains(request.Id));
        if (nextRequest is null)
        {
            return "All requests completed.";
        }

        var currentQuantity = inventory.GetQuantity(nextRequest.RequiredItemId);
        var itemDisplayName = ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog);
        if (currentQuantity >= nextRequest.RequiredQuantity)
        {
            return $"Request ready: {itemDisplayName} {currentQuantity}/{nextRequest.RequiredQuantity}. Click board to turn in.";
        }

        var remainingQuantity = nextRequest.RequiredQuantity - currentQuantity;
        return $"Active request: {itemDisplayName} {currentQuantity}/{nextRequest.RequiredQuantity}. Need {remainingQuantity} more.";
    }

    public static bool TryHandleFarmPlotInteraction(
        FarmGrid farmGrid,
        InventoryState inventory,
        IReadOnlyDictionary<string, CropDefinition> crops,
        int x,
        int y,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(farmGrid);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(crops);

        var plot = farmGrid.GetPlot(x, y);
        if (plot.IsLocked)
        {
            message = "Plot is locked.";
            return false;
        }

        if (!plot.IsTilled)
        {
            farmGrid.SetPlot(plot.Till());
            var cropToPlant = FindAutoPlantCrop(crops, inventory);
            message = cropToPlant is null
                ? "Plot tilled. No seeds available."
                : $"Plot tilled. Click again to plant {cropToPlant.DisplayName}.";
            return true;
        }

        if (plot.Crop is null)
        {
            var cropToPlant = FindAutoPlantCrop(crops, inventory);

            if (cropToPlant is null)
            {
                message = "No seeds available.";
                return false;
            }

            if (!inventory.TryRemove(cropToPlant.SeedItemId, 1))
            {
                message = "No seeds available.";
                return false;
            }

            farmGrid.SetPlot(plot.Plant(cropToPlant.Id));
            message = $"Planted {cropToPlant.DisplayName}. Click again to water.";
            return true;
        }

        if (!crops.TryGetValue(plot.Crop.CropId, out var cropDefinition))
        {
            throw new InvalidOperationException($"Unknown crop id '{plot.Crop.CropId}' in plot state.");
        }

        if (plot.IsHarvestReady)
        {
            if (!inventory.TryAdd(cropDefinition.HarvestItemId, 1))
            {
                message = $"Cannot harvest {cropDefinition.DisplayName}: inventory full.";
                return false;
            }

            farmGrid.SetPlot(plot with
            {
                Crop = null,
                IsWateredToday = false,
                IsHarvestReady = false
            });

            message = $"Harvested {cropDefinition.DisplayName}.";
            return true;
        }

        if (!plot.IsWateredToday)
        {
            farmGrid.SetPlot(plot.Water());
            message = $"Watered {cropDefinition.DisplayName}.";
            return true;
        }

        message = $"{cropDefinition.DisplayName} already watered today.";
        return false;
    }

    private static CropDefinition? FindAutoPlantCrop(
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState inventory)
    {
        ArgumentNullException.ThrowIfNull(crops);
        ArgumentNullException.ThrowIfNull(inventory);

        return crops.Values
            .OrderBy(static crop => crop.DisplayName, StringComparer.Ordinal)
            .FirstOrDefault(crop => inventory.GetQuantity(crop.SeedItemId) > 0);
    }

    public static bool TryTransferItem(InventoryState source, InventoryState destination, string itemId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        if (source.GetQuantity(itemId) < quantity)
        {
            return false;
        }

        var sourceSnapshot = source.CreateSnapshot();
        var destinationSnapshot = destination.CreateSnapshot();

        if (!source.TryRemove(itemId, quantity))
        {
            return false;
        }

        if (destination.TryAdd(itemId, quantity))
        {
            return true;
        }

        source.RestoreSnapshot(sourceSnapshot);
        destination.RestoreSnapshot(destinationSnapshot);
        return false;
    }

    public static bool TryCompleteNextRequest(
        IReadOnlyList<RequestDefinition> requests,
        RequestBoardService requestBoardService,
        InventoryState inventory,
        ISet<string> completedRequestIds,
        Wallet wallet,
        out string message)
    {
        return TryCompleteNextRequest(
            requests,
            requestBoardService,
            inventory,
            completedRequestIds,
            wallet,
            itemCatalog: null,
            out message);
    }

    public static bool TryCompleteNextRequest(
        IReadOnlyList<RequestDefinition> requests,
        RequestBoardService requestBoardService,
        InventoryState inventory,
        ISet<string> completedRequestIds,
        Wallet wallet,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(requestBoardService);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(wallet);

        var nextRequest = requests.FirstOrDefault(request => !completedRequestIds.Contains(request.Id));
        if (nextRequest is null)
        {
            message = "All requests completed.";
            return false;
        }

        if (!requestBoardService.TryComplete(nextRequest, inventory, completedRequestIds, out var rewardGold))
        {
            var currentQuantity = inventory.GetQuantity(nextRequest.RequiredItemId);
            var remainingQuantity = Math.Max(0, nextRequest.RequiredQuantity - currentQuantity);
            var itemDisplayName = ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog);
            message = $"Need {remainingQuantity} more {itemDisplayName}.";
            return false;
        }

        wallet.Earn(rewardGold);
        message = itemCatalog is null
            ? $"Completed request {nextRequest.Id} for {rewardGold}g."
            : $"Completed request: delivered {nextRequest.RequiredQuantity} {ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog)} for {rewardGold}g.";
        return true;
    }
}
