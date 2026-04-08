namespace HarvestManor.Core.Content;

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string Category,
    int MaxStack);
