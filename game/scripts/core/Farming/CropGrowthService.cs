using HarvestManor.Core.Content;

namespace HarvestManor.Core.Farming;

public sealed class CropGrowthService
{
    private readonly IReadOnlyDictionary<string, CropDefinition> _crops;

    public CropGrowthService(IReadOnlyDictionary<string, CropDefinition> crops)
    {
        _crops = crops ?? throw new ArgumentNullException(nameof(crops));
    }

    public PlotState AdvanceDay(PlotState plot)
    {
        if (plot.Crop is null || !plot.IsWateredToday)
        {
            return plot with { IsWateredToday = false };
        }

        if (!_crops.TryGetValue(plot.Crop.CropId, out var crop))
        {
            throw new InvalidOperationException($"Unknown crop id '{plot.Crop.CropId}' in plot state.");
        }

        var nextDays = plot.Crop.DaysGrown + 1;

        return plot with
        {
            Crop = plot.Crop with { DaysGrown = nextDays },
            IsWateredToday = false,
            IsHarvestReady = nextDays >= crop.TotalGrowthDays
        };
    }
}
