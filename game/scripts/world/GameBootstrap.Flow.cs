using System.Linq;
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
    public readonly record struct DayEndResult(bool DayRolled, bool SeasonChanged, Season PreviousSeason, Season CurrentSeason, int CropsWithered);

    public static DayEndResult ProcessDayEnd(
        DayClock clock,
        StaminaState stamina,
        CropGrowthService growth,
        FarmGrid farmGrid,
        IReadOnlyDictionary<string, CropDefinition> cropCatalog,
        int minutesToAdvance = 20 * 60)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(stamina);
        ArgumentNullException.ThrowIfNull(growth);
        ArgumentNullException.ThrowIfNull(farmGrid);
        ArgumentNullException.ThrowIfNull(cropCatalog);

        var previousSeason = clock.Date.Season;
        var rolled = clock.AdvanceMinutes(minutesToAdvance);
        if (!rolled)
        {
            return new DayEndResult(false, false, previousSeason, previousSeason, 0);
        }

        var currentSeason = clock.Date.Season;
        var seasonChanged = previousSeason != currentSeason;
        var cropsWithered = 0;

        foreach (var plot in farmGrid.AllPlots.ToList())
        {
            if (seasonChanged && plot.Crop is not null)
            {
                var cropSeason = cropCatalog.TryGetValue(plot.Crop.CropId, out var def) ? def.Season : previousSeason;
                if (cropSeason != currentSeason)
                {
                    farmGrid.SetPlot(plot with { Crop = null, IsHarvestReady = false, IsWateredToday = false });
                    cropsWithered++;
                    continue;
                }
            }

            farmGrid.SetPlot(growth.AdvanceDay(plot));
        }

        stamina.RestoreFull();
        return new DayEndResult(true, seasonChanged, previousSeason, currentSeason, cropsWithered);
    }

    public static IReadOnlyList<ShopOffer> BuildSeasonShopOffers(
        IReadOnlyList<ShopOffer> allOffers,
        IReadOnlyDictionary<string, CropDefinition> cropCatalog,
        Season currentSeason)
    {
        ArgumentNullException.ThrowIfNull(allOffers);
        ArgumentNullException.ThrowIfNull(cropCatalog);

        var seasonSeedIds = new HashSet<string>(
            cropCatalog.Values
                .Where(c => c.Season == currentSeason)
                .Select(c => c.SeedItemId),
            StringComparer.Ordinal);

        var seasonCropIds = new HashSet<string>(
            cropCatalog.Values
                .Select(c => c.HarvestItemId),
            StringComparer.Ordinal);

        return allOffers
            .Where(offer =>
                seasonSeedIds.Contains(offer.ItemId)
                || seasonCropIds.Contains(offer.ItemId))
            .ToList();
    }

    public static string GetLockedPlotHint(int x, int y, ExpansionTierService tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);

        var cost = tiers.GetUnlockCost(x, y);
        return cost is null
            ? "Locked"
            : $"Click: unlock ({cost.Value}g)";
    }

    public static bool TryHandleLockedPlotInteraction(
        FarmExpansionService expansionService,
        ExpansionTierService tiers,
        UnlockState unlockState,
        Wallet wallet,
        int x,
        int y,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(expansionService);
        ArgumentNullException.ThrowIfNull(tiers);
        ArgumentNullException.ThrowIfNull(unlockState);
        ArgumentNullException.ThrowIfNull(wallet);

        var plotKey = BuildPlotKey(x, y);
        var cost = tiers.GetUnlockCost(x, y);
        if (cost is null)
        {
            message = "Plot is locked.";
            return false;
        }

        var requiredCost = cost.Value;
        if (expansionService.TryUnlockPlot(unlockState, plotKey, requiredCost, wallet))
        {
            message = $"Unlocked a new plot for {requiredCost}g. Click again to till.";
            return true;
        }

        message = wallet.Gold < requiredCost
            ? $"Need {requiredCost}g to unlock this plot."
            : "Plot is locked.";
        return false;
    }

    private const int FarmActionStaminaCost = 5;

    public static bool TryHandleFarmPlotInteraction(
        FarmGrid farmGrid,
        InventoryState inventory,
        IReadOnlyDictionary<string, CropDefinition> crops,
        int x,
        int y,
        out string message,
        StaminaState? stamina = null,
        Season? currentSeason = null)
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

        if (stamina is not null && stamina.Current < FarmActionStaminaCost)
        {
            message = "Too tired. Rest to restore stamina.";
            return false;
        }

        if (!plot.IsTilled)
        {
            farmGrid.SetPlot(plot.Till());
            stamina?.TrySpend(FarmActionStaminaCost);
            var cropToPlant = FindAutoPlantCrop(crops, inventory, currentSeason);
            message = cropToPlant is null
                ? $"Plot tilled. {NoSeedsMessage(crops, inventory, currentSeason)}"
                : $"Plot tilled. Click again to plant {cropToPlant.DisplayName}.";
            return true;
        }

        if (plot.Crop is null)
        {
            var cropToPlant = FindAutoPlantCrop(crops, inventory, currentSeason);

            if (cropToPlant is null)
            {
                message = NoSeedsMessage(crops, inventory, currentSeason);
                return false;
            }

            if (!inventory.TryRemove(cropToPlant.SeedItemId, 1))
            {
                message = "No seeds available.";
                return false;
            }

            farmGrid.SetPlot(plot.Plant(cropToPlant.Id));
            stamina?.TrySpend(FarmActionStaminaCost);
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

            farmGrid.SetPlot(plot.Harvest());
            stamina?.TrySpend(FarmActionStaminaCost);

            message = $"Harvested {cropDefinition.DisplayName}.";
            return true;
        }

        if (!plot.IsWateredToday)
        {
            farmGrid.SetPlot(plot.Water());
            stamina?.TrySpend(FarmActionStaminaCost);
            message = $"Watered {cropDefinition.DisplayName}.";
            return true;
        }

        message = $"{cropDefinition.DisplayName} already watered today.";
        return false;
    }

    public static CropDefinition? FindAutoPlantCrop(
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState inventory,
        Season? currentSeason = null)
    {
        ArgumentNullException.ThrowIfNull(crops);
        ArgumentNullException.ThrowIfNull(inventory);

        return crops.Values
            .Where(crop => currentSeason is null || crop.Season == currentSeason.Value)
            .OrderBy(static crop => crop.DisplayName, StringComparer.Ordinal)
            .FirstOrDefault(crop => inventory.GetQuantity(crop.SeedItemId) > 0);
    }

    public static bool HasAnySeeds(
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState inventory)
    {
        ArgumentNullException.ThrowIfNull(crops);
        ArgumentNullException.ThrowIfNull(inventory);

        return crops.Values.Any(crop => inventory.GetQuantity(crop.SeedItemId) > 0);
    }

    internal static string NoSeedsMessage(
        IReadOnlyDictionary<string, CropDefinition> crops,
        InventoryState inventory,
        Season? currentSeason)
    {
        if (currentSeason is not null && HasAnySeeds(crops, inventory))
        {
            return $"No {currentSeason.Value} seeds available. Your seeds are for a different season.";
        }

        return "No seeds available.";
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
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(requestBoardService);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(completedRequestIds);
        ArgumentNullException.ThrowIfNull(wallet);

        var nextRequest = RequestBoardService.FindNextPendingRequest(requests, completedRequestIds);
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
