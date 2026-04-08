using HarvestManor.Core.Content;
using HarvestManor.Core.Farming;
using Xunit;

namespace HarvestManor.Game.Tests.Farming;

public sealed class CropGrowthServiceTests
{
    [Fact]
    public void AdvanceDay_GrowsWateredCropByOneDay()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            "Spring",
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var plot = PlotState.Tilled(0, 0).Plant(crop.Id).Water();
        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });

        var next = growth.AdvanceDay(plot);

        Assert.Equal(1, next.Crop!.DaysGrown);
        Assert.False(next.IsWateredToday);
        Assert.False(next.IsHarvestReady);
    }

    [Fact]
    public void AdvanceDay_MarksCropHarvestReadyAtGrowthLimit()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            "Spring",
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var plot = PlotState.Tilled(0, 0).Plant(crop.Id).Water();
        plot = plot with { Crop = plot.Crop! with { DaysGrown = 3 } };

        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });
        var next = growth.AdvanceDay(plot);

        Assert.True(next.IsHarvestReady);
    }
}
