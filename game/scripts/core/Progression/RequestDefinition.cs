namespace HarvestManor.Core.Progression;

public sealed record RequestDefinition(string Id, string RequiredItemId, int RequiredQuantity, int RewardGold);
