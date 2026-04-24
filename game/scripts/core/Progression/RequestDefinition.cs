namespace HarvestManor.Core.Progression;

public sealed record RequestDefinition(string Id, string RequiredItemId, int RequiredQuantity, int RewardGold)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidDataException("Request definition has a blank id.");
        }

        if (string.IsNullOrWhiteSpace(RequiredItemId))
        {
            throw new InvalidDataException($"Request '{Id}' has a blank required item id.");
        }

        if (RequiredQuantity <= 0)
        {
            throw new InvalidDataException($"Request '{Id}' has an invalid required quantity ({RequiredQuantity}).");
        }

        if (RewardGold < 0)
        {
            throw new InvalidDataException($"Request '{Id}' has a negative reward gold ({RewardGold}).");
        }
    }
}
