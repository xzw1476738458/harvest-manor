using System.Linq;

namespace HarvestManor.Core.Content;

public sealed record CropDefinition(
    string Id,
    string DisplayName,
    string Season,
    string SeedItemId,
    string HarvestItemId,
    int PurchasePrice,
    int SellPrice,
    int TotalGrowthDays,
    IReadOnlyList<int> GrowthStageDays)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidDataException("Crop id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidDataException($"Crop '{Id}' has an empty display name.");
        }

        if (string.IsNullOrWhiteSpace(Season))
        {
            throw new InvalidDataException($"Crop '{Id}' has an empty season.");
        }

        if (string.IsNullOrWhiteSpace(SeedItemId))
        {
            throw new InvalidDataException($"Crop '{Id}' has an empty seed item id.");
        }

        if (string.IsNullOrWhiteSpace(HarvestItemId))
        {
            throw new InvalidDataException($"Crop '{Id}' has an empty harvest item id.");
        }

        if (PurchasePrice < 0 || SellPrice < 0)
        {
            throw new InvalidDataException($"Crop '{Id}' has invalid prices.");
        }

        if (TotalGrowthDays <= 0)
        {
            throw new InvalidDataException($"Crop '{Id}' has invalid total growth days.");
        }

        if (GrowthStageDays is null || GrowthStageDays.Count == 0 || GrowthStageDays.Any(days => days <= 0) || GrowthStageDays.Sum() != TotalGrowthDays)
        {
            throw new InvalidDataException($"Crop '{Id}' has invalid growth stage totals.");
        }
    }
}
