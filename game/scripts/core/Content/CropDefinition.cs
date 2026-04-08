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

        if (GrowthStageDays is null || GrowthStageDays.Count == 0 || GrowthStageDays.Any(days => days <= 0) || GrowthStageDays.Sum() != TotalGrowthDays)
        {
            throw new InvalidDataException($"Crop '{Id}' has invalid growth stage totals.");
        }
    }
}
