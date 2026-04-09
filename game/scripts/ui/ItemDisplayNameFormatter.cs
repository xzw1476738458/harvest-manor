using HarvestManor.Core.Content;

namespace HarvestManor.UI;

internal static class ItemDisplayNameFormatter
{
    public static string Resolve(string itemId, IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return string.Empty;
        }

        return itemCatalog is not null && itemCatalog.TryGetValue(itemId, out var definition)
            ? definition.DisplayName
            : itemId;
    }
}
