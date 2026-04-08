namespace HarvestManor.Core.Content;

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string Category,
    int MaxStack)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidDataException("Item id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidDataException($"Item '{Id}' has an empty display name.");
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            throw new InvalidDataException($"Item '{Id}' has an empty category.");
        }

        if (MaxStack <= 0)
        {
            throw new InvalidDataException($"Item '{Id}' has invalid max stack.");
        }
    }
}
