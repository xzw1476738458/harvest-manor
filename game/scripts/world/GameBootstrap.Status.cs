using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap
{
    public static string BuildStartupFarmStatusMessage(bool saveFileExists, bool loadedExistingSave)
    {
        return BuildStartupFarmStatusMessage(saveFileExists, loadedExistingSave, farmGrid: null);
    }

    public static string BuildStartupFarmStatusMessage(bool saveFileExists, bool loadedExistingSave, FarmGrid? farmGrid)
    {
        return BuildStartupFarmStatusMessage(
            saveFileExists,
            loadedExistingSave,
            farmGrid,
            requests: null,
            completedRequestIds: null,
            inventory: null,
            itemCatalog: null);
    }

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
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        var currentFarmStatus = BuildPriorityFarmStatusMessage(farmGrid, requests, completedRequestIds, inventory, itemCatalog);
        if (!string.IsNullOrWhiteSpace(currentFarmStatus))
        {
            return $"A new day begins. {currentFarmStatus}";
        }

        return "A new day begins. Click a plot to till, plant, water, or harvest.";
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

    public static PanelVisibility ResolvePanelVisibility(PanelMode mode)
    {
        return mode switch
        {
            PanelMode.Shop => new PanelVisibility(false, true, false),
            PanelMode.Storage => new PanelVisibility(true, false, true),
            _ => new PanelVisibility(false, false, false)
        };
    }

    public static bool BlocksWorldInteractions(PanelMode mode)
    {
        return mode != PanelMode.None;
    }

    public static PanelMode ResolvePanelModeAfterUnhandledKey(PanelMode currentMode, Key keycode)
    {
        return keycode == Key.Escape && currentMode != PanelMode.None
            ? PanelMode.None
            : currentMode;
    }

    public static bool CanHandlePanelInteractionRequest(PanelMode currentMode, PanelMode requestedMode)
    {
        return requestedMode != PanelMode.None
            && (currentMode == PanelMode.None || currentMode == requestedMode);
    }

    public static PanelMode ResolvePanelModeAfterInteractionRequest(PanelMode currentMode, PanelMode requestedMode)
    {
        return currentMode == requestedMode
            ? PanelMode.None
            : requestedMode;
    }

    public static bool CanTriggerDemoExpansionShortcut(PanelMode currentMode, Key keycode)
    {
        return keycode == Key.F7 && !BlocksWorldInteractions(currentMode);
    }

    public static string? BuildBlockedWorldInteractionMessage(PanelMode mode, PanelMode requestedMode = PanelMode.None)
    {
        if (mode != PanelMode.None && mode == requestedMode)
        {
            return mode switch
            {
                PanelMode.Shop => "Shop open. Click again or press Esc to close.",
                PanelMode.Storage => "Storage open. Click again or press Esc to close.",
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

    public static string BuildFarmPlotHoverStatusMessage(
        PlotState plot,
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState? inventory = null,
        int? currentGold = null)
    {
        ArgumentNullException.ThrowIfNull(plot);
        ArgumentNullException.ThrowIfNull(crops);

        if (plot.IsLocked)
        {
            if (BuildPlotKey(plot.X, plot.Y) == DemoExpansionPlotKey && currentGold is not null && currentGold < DemoExpansionCost)
            {
                return $"Hover plot: need {DemoExpansionCost}g to unlock.";
            }

            return BuildPlotKey(plot.X, plot.Y) == DemoExpansionPlotKey
                ? $"Hover plot: unlock for {DemoExpansionCost}g."
                : "Hover plot: locked.";
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

            var cropToPlant = FindAutoPlantCrop(crops, inventory);
            if (cropToPlant is null)
            {
                return "Hover plot: no seeds available.";
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

    public static string BuildRequestBoardHoverStatusMessage(
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
}
