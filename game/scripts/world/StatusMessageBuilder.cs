using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Gathering;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public static class StatusMessageBuilder
{
    public static string BuildStartupFarmStatusMessage(
        bool saveFileExists,
        bool loadedExistingSave,
        FarmGrid? farmGrid,
        IReadOnlyList<RequestDefinition>? requests,
        ISet<string>? completedRequestIds,
        InventoryState? inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        if (loadedExistingSave)
        {
            var currentFarmStatus = BuildPriorityFarmStatusMessage(farmGrid, requests, completedRequestIds, inventory, itemCatalog);
            if (!string.IsNullOrWhiteSpace(currentFarmStatus))
            {
                return $"Save loaded. {currentFarmStatus}";
            }

            return "Save loaded. Click a plot to till, plant, water, or harvest.";
        }

        return saveFileExists
            ? "Previous save could not be read. Started a fresh day instead."
            : "Fresh start. Click a plot to till, plant, water, or harvest.";
    }

    public static string BuildDayStartFarmStatusMessage(
        FarmGrid? farmGrid,
        IReadOnlyList<RequestDefinition>? requests = null,
        ISet<string>? completedRequestIds = null,
        InventoryState? inventory = null,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null,
        Season? newSeason = null,
        int cropsWithered = 0)
    {
        var prefix = newSeason is not null
            ? $"{newSeason.Value} has arrived!"
            : "A new day begins.";

        if (newSeason is not null && cropsWithered > 0)
        {
            prefix += $" {cropsWithered} out-of-season {(cropsWithered == 1 ? "crop has" : "crops have")} withered.";
        }

        var currentFarmStatus = BuildPriorityFarmStatusMessage(farmGrid, requests, completedRequestIds, inventory, itemCatalog);
        if (!string.IsNullOrWhiteSpace(currentFarmStatus))
        {
            return $"{prefix} {currentFarmStatus}";
        }

        return $"{prefix} Click a plot to till, plant, water, or harvest.";
    }

    private static string? BuildPriorityFarmStatusMessage(
        FarmGrid? farmGrid,
        IReadOnlyList<RequestDefinition>? requests,
        ISet<string>? completedRequestIds,
        InventoryState? inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        if (farmGrid is not null)
        {
            var harvestReadyCount = farmGrid.AllPlots.Count(static plot => plot.IsHarvestReady);
            if (harvestReadyCount > 0)
            {
                return $"{harvestReadyCount} {(harvestReadyCount == 1 ? "crop is" : "crops are")} ready to harvest.";
            }

            var waterNeededCount = farmGrid.AllPlots.Count(static plot => plot.Crop is not null && !plot.IsHarvestReady && !plot.IsWateredToday);
            if (waterNeededCount > 0)
            {
                return $"{waterNeededCount} planted {(waterNeededCount == 1 ? "crop still needs" : "crops still need")} water.";
            }
        }

        if (requests is not null && completedRequestIds is not null && inventory is not null)
        {
            var requestStatus = BuildRequestBoardStatusText(requests, completedRequestIds, inventory, itemCatalog);
            if (!string.IsNullOrWhiteSpace(requestStatus))
            {
                return requestStatus;
            }
        }

        return null;
    }

    public static string? BuildBlockedWorldInteractionMessage(PanelMode mode, PanelMode requestedMode = PanelMode.None)
    {
        if (mode != PanelMode.None && mode == requestedMode)
        {
            return mode switch
            {
                PanelMode.Shop => "Shop open. Click again or press Esc to close.",
                PanelMode.Storage => "Storage open. Click again or press Esc to close.",
                PanelMode.Inventory => "Inventory open. Press Tab or Esc to close.",
                _ => null
            };
        }

        if (mode == PanelMode.Shop && requestedMode == PanelMode.Storage)
        {
            return "Close the shop panel before opening storage.";
        }

        if (mode == PanelMode.Storage && requestedMode == PanelMode.Shop)
        {
            return "Close the storage panel before opening shop.";
        }

        return mode switch
        {
            PanelMode.Shop => "Close the shop panel before interacting with the world.",
            PanelMode.Storage => "Close the storage panel before interacting with the world.",
            PanelMode.Inventory => "Close the inventory before interacting with the world.",
            _ => null
        };
    }

    public static string? BuildPanelModeStatusMessage(PanelMode previousMode, PanelMode nextMode)
    {
        if (previousMode == nextMode)
        {
            return null;
        }

        return nextMode switch
        {
            PanelMode.Shop => "Shop open. Use Buy/Sell or press Esc to close.",
            PanelMode.Storage => "Storage open. Move items or press Esc to close.",
            PanelMode.Inventory => "Inventory open. Press Tab or Esc to close.",
            _ => null
        };
    }

    public static string BuildPanelCloseStatusMessage(PanelMode previousMode, string? latestPanelContextMessage)
    {
        if (previousMode != PanelMode.None && !string.IsNullOrWhiteSpace(latestPanelContextMessage))
        {
            return latestPanelContextMessage;
        }

        return "Panels closed. Interact with the world again.";
    }

    public static string BuildGatheringStatusMessage(GatheringHarvestResult result, string? itemDisplayName)
    {
        ArgumentNullException.ThrowIfNull(result);

        var name = string.IsNullOrWhiteSpace(itemDisplayName) ? "resource" : itemDisplayName!.ToLowerInvariant();
        var titleCaseName = string.IsNullOrWhiteSpace(itemDisplayName) ? "Resource" : itemDisplayName!;

        return result.Outcome switch
        {
            GatheringHarvestOutcome.Success => $"Gathered +1 {name}.",
            GatheringHarvestOutcome.AlreadyHarvested => $"{titleCaseName} already gathered today.",
            GatheringHarvestOutcome.InventoryFull => $"Inventory full: cannot pick up {name}.",
            GatheringHarvestOutcome.UnknownNode => "Unknown gathering spot.",
            _ => string.Empty,
        };
    }

    public static string BuildFarmPlotHoverStatusMessage(
        PlotState plot,
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState? inventory = null,
        int? currentGold = null,
        Season? currentSeason = null,
        Func<int, int, int?>? lookupUnlockCost = null)
    {
        ArgumentNullException.ThrowIfNull(plot);
        ArgumentNullException.ThrowIfNull(crops);

        if (plot.IsLocked)
        {
            var unlockCost = lookupUnlockCost?.Invoke(plot.X, plot.Y);
            if (unlockCost is null)
            {
                return "Hover plot: locked.";
            }

            if (currentGold is not null && currentGold < unlockCost.Value)
            {
                return $"Hover plot: need {unlockCost.Value}g to unlock.";
            }

            return $"Hover plot: unlock for {unlockCost.Value}g.";
        }

        if (!plot.IsTilled)
        {
            return "Hover plot: click to till.";
        }

        if (plot.Crop is null)
        {
            if (inventory is null)
            {
                return "Hover plot: click to plant.";
            }

            var cropToPlant = GameBootstrap.FindAutoPlantCrop(crops, inventory, currentSeason);
            if (cropToPlant is null)
            {
                var reason = currentSeason is not null && GameBootstrap.HasAnySeeds(crops, inventory)
                    ? $"no {currentSeason.Value} seeds available"
                    : "no seeds available";
                return $"Hover plot: {reason}.";
            }

            return $"Hover plot: click to plant {cropToPlant.DisplayName}.";
        }

        var cropName = crops.TryGetValue(plot.Crop.CropId, out var crop)
            ? crop.DisplayName
            : plot.Crop.CropId;

        if (plot.IsHarvestReady && crop is not null && inventory is not null && !inventory.CanAdd(crop.HarvestItemId, 1))
        {
            return $"Hover {cropName}: inventory full.";
        }

        if (plot.IsHarvestReady)
        {
            return $"Hover {cropName}: ready to harvest.";
        }

        if (plot.IsWateredToday)
        {
            return $"Hover {cropName}: watered today.";
        }

        return $"Hover {cropName}: click to water.";
    }

    public static string BuildInteractionHoverStatusMessage(string interactionName, string actionDescription)
    {
        if (string.IsNullOrWhiteSpace(interactionName))
        {
            throw new ArgumentException("Interaction name cannot be blank.", nameof(interactionName));
        }

        if (string.IsNullOrWhiteSpace(actionDescription))
        {
            throw new ArgumentException("Action description cannot be blank.", nameof(actionDescription));
        }

        return $"Hover {interactionName}: {actionDescription}.";
    }

    public static string BuildShopClosedHoverStatusMessage()
    {
        return $"Hover general store: closed (open {TimeOfDayController.FormatShopHours()}).";
    }

    public static string BuildShopClosedAttemptStatusMessage()
    {
        return $"The general store is closed. Come back between {TimeOfDayController.FormatShopHours()}.";
    }

    public static string BuildRequestBoardHoverStatusMessage(
        IReadOnlyList<RequestDefinition> requests,
        ISet<string> completedRequestIds,
        InventoryState inventory,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(inventory);

        var nextRequest = RequestBoardService.FindNextPendingRequest(requests, completedRequestIds);
        if (nextRequest is null)
        {
            return "Hover request board: all requests completed.";
        }

        var currentQuantity = inventory.GetQuantity(nextRequest.RequiredItemId);
        var itemDisplayName = ItemDisplayNameFormatter.Resolve(nextRequest.RequiredItemId, itemCatalog);
        if (currentQuantity >= nextRequest.RequiredQuantity)
        {
            return $"Hover request board: {itemDisplayName} {currentQuantity}/{nextRequest.RequiredQuantity} ready to turn in for {nextRequest.RewardGold}g.";
        }

        var remainingQuantity = nextRequest.RequiredQuantity - currentQuantity;
        return $"Hover request board: {itemDisplayName} {currentQuantity}/{nextRequest.RequiredQuantity}. Need {remainingQuantity} more.";
    }

    public static string BuildShopBrowseStatusMessage(
        IReadOnlyList<ShopOffer> offers,
        int selectedOfferIndex,
        InventoryState inventory,
        Wallet wallet,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(offers);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(wallet);

        if (offers.Count == 0)
        {
            return "Shop open. No offers available.";
        }

        var clampedIndex = Math.Clamp(selectedOfferIndex, 0, offers.Count - 1);
        var offer = offers[clampedIndex];
        var state = ShopPanelController.EvaluateOfferState(offer, inventory, wallet);
        return $"Shop selection: {ItemDisplayNameFormatter.Resolve(offer.ItemId, itemCatalog)}. {state.StatusText}";
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

        var nextRequest = RequestBoardService.FindNextPendingRequest(requests, completedRequestIds);
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
}
