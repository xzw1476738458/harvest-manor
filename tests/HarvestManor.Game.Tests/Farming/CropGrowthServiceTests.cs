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

    [Fact]
    public void SetPlot_ThrowsWhenCoordinatesAreOutOfBounds()
    {
        var grid = new FarmGrid(2, 2);
        var outOfBounds = PlotState.Wild(2, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetPlot(outOfBounds));
    }

    [Fact]
    public void GetPlot_ThrowsWhenCoordinatesAreOutOfBounds()
    {
        var grid = new FarmGrid(2, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetPlot(-1, 0));
    }

    [Fact]
    public void AdvanceDay_ThrowsControlledErrorWhenCropIdIsUnknown()
    {
        var growth = new CropGrowthService(new Dictionary<string, CropDefinition>());
        var plot = PlotState.Tilled(0, 0).Plant("unknown_crop").Water();

        var exception = Assert.Throws<InvalidOperationException>(() => growth.AdvanceDay(plot));

        Assert.Contains("unknown_crop", exception.Message);
    }

    [Fact]
    public void Plant_ThrowsWhenPlotIsWild()
    {
        var wild = PlotState.Wild(0, 0);

        Assert.Throws<InvalidOperationException>(() => wild.Plant("parsnip"));
    }

    [Fact]
    public void Plant_ThrowsWhenPlotAlreadyHasCrop()
    {
        var planted = PlotState.Tilled(0, 0).Plant("parsnip");

        Assert.Throws<InvalidOperationException>(() => planted.Plant("potato"));
    }

    [Fact]
    public void Water_ThrowsWhenPlotHasNoCrop()
    {
        var tilled = PlotState.Tilled(0, 0);

        Assert.Throws<InvalidOperationException>(() => tilled.Water());
    }

    [Fact]
    public void Water_ThrowsWhenPlotIsAlreadyWatered()
    {
        var watered = PlotState.Tilled(0, 0).Plant("parsnip").Water();

        Assert.Throws<InvalidOperationException>(() => watered.Water());
    }

    [Fact]
    public void Water_ThrowsWhenPlotIsLocked()
    {
        var locked = PlotState.Tilled(0, 0) with { IsLocked = true };

        Assert.Throws<InvalidOperationException>(() => locked.Water());
    }
}
