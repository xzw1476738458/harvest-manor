namespace HarvestManor.Core.Gathering;

public sealed record GatheringNodeDefinition(string NodeId, string ItemId)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(NodeId))
        {
            throw new InvalidDataException("Gathering node id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(ItemId))
        {
            throw new InvalidDataException($"Gathering node '{NodeId}' has no item id.");
        }
    }
}
