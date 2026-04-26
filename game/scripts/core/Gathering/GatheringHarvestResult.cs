namespace HarvestManor.Core.Gathering;

public enum GatheringHarvestOutcome
{
    Success,
    UnknownNode,
    AlreadyHarvested,
    InventoryFull
}

public sealed record GatheringHarvestResult(GatheringHarvestOutcome Outcome, string? ItemId);
