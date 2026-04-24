using HarvestManor.Core.Farming;
using Xunit;

namespace HarvestManor.Game.Tests.Farming;

public sealed class PlotStateTests
{
    [Fact]
    public void Wild_CreatesUntilledUnlockedPlot()
    {
        var plot = PlotState.Wild(3, 5);

        Assert.Equal(3, plot.X);
        Assert.Equal(5, plot.Y);
        Assert.False(plot.IsTilled);
        Assert.False(plot.IsLocked);
        Assert.False(plot.IsWateredToday);
        Assert.False(plot.IsHarvestReady);
        Assert.Null(plot.Crop);
    }

    [Fact]
    public void Tilled_CreatesTilledPlotWithNoCrop()
    {
        var plot = PlotState.Tilled(1, 2);

        Assert.True(plot.IsTilled);
        Assert.Null(plot.Crop);
        Assert.False(plot.IsWateredToday);
        Assert.False(plot.IsHarvestReady);
    }

    [Fact]
    public void Till_SetsIsTilled()
    {
        var plot = PlotState.Wild(0, 0);
        var tilled = plot.Till();

        Assert.True(tilled.IsTilled);
        Assert.False(plot.IsTilled);
    }

    [Fact]
    public void Till_ThrowsWhenLocked()
    {
        var plot = PlotState.Wild(0, 0) with { IsLocked = true };

        Assert.Throws<InvalidOperationException>(() => plot.Till());
    }

    [Fact]
    public void Plant_SetsCropAndResetsFlags()
    {
        var plot = PlotState.Tilled(0, 0);
        var planted = plot.Plant("parsnip");

        Assert.NotNull(planted.Crop);
        Assert.Equal("parsnip", planted.Crop.CropId);
        Assert.Equal(0, planted.Crop.DaysGrown);
        Assert.False(planted.IsWateredToday);
        Assert.False(planted.IsHarvestReady);
    }

    [Fact]
    public void Plant_ThrowsWhenCropIdIsBlank()
    {
        var plot = PlotState.Tilled(0, 0);

        Assert.Throws<ArgumentException>(() => plot.Plant(""));
        Assert.Throws<ArgumentException>(() => plot.Plant("  "));
    }

    [Fact]
    public void Plant_ThrowsWhenLocked()
    {
        var plot = PlotState.Tilled(0, 0) with { IsLocked = true };

        Assert.Throws<InvalidOperationException>(() => plot.Plant("parsnip"));
    }

    [Fact]
    public void Plant_ThrowsWhenUntilled()
    {
        var plot = PlotState.Wild(0, 0);

        Assert.Throws<InvalidOperationException>(() => plot.Plant("parsnip"));
    }

    [Fact]
    public void Plant_ThrowsWhenPlotAlreadyHasCrop()
    {
        var plot = PlotState.Tilled(0, 0).Plant("parsnip");

        Assert.Throws<InvalidOperationException>(() => plot.Plant("potato"));
    }

    [Fact]
    public void Water_SetsIsWateredToday()
    {
        var plot = PlotState.Tilled(0, 0).Plant("parsnip");
        var watered = plot.Water();

        Assert.True(watered.IsWateredToday);
        Assert.False(plot.IsWateredToday);
    }

    [Fact]
    public void Water_ThrowsWhenLocked()
    {
        var plot = PlotState.Tilled(0, 0).Plant("parsnip") with { IsLocked = true };

        Assert.Throws<InvalidOperationException>(() => plot.Water());
    }

    [Fact]
    public void Water_ThrowsWhenUntilled()
    {
        var plot = PlotState.Wild(0, 0);

        Assert.Throws<InvalidOperationException>(() => plot.Water());
    }

    [Fact]
    public void Water_ThrowsWhenNoCrop()
    {
        var plot = PlotState.Tilled(0, 0);

        Assert.Throws<InvalidOperationException>(() => plot.Water());
    }

    [Fact]
    public void Water_ThrowsWhenAlreadyWatered()
    {
        var plot = PlotState.Tilled(0, 0).Plant("parsnip").Water();

        Assert.Throws<InvalidOperationException>(() => plot.Water());
    }

    [Fact]
    public void Harvest_ResetsCropAndFlags()
    {
        var plot = new PlotState(0, 0, IsTilled: true, IsLocked: false, IsWateredToday: true, IsHarvestReady: true, Crop: new CropInstance("parsnip", 4));
        var harvested = plot.Harvest();

        Assert.Null(harvested.Crop);
        Assert.False(harvested.IsWateredToday);
        Assert.False(harvested.IsHarvestReady);
        Assert.True(harvested.IsTilled);
    }

    [Fact]
    public void Harvest_ThrowsWhenNotReady()
    {
        var plot = new PlotState(0, 0, IsTilled: true, IsLocked: false, IsWateredToday: true, IsHarvestReady: false, Crop: new CropInstance("parsnip", 2));

        Assert.Throws<InvalidOperationException>(() => plot.Harvest());
    }

    [Fact]
    public void Harvest_ThrowsWhenLocked()
    {
        var plot = new PlotState(0, 0, IsTilled: true, IsLocked: true, IsWateredToday: true, IsHarvestReady: true, Crop: new CropInstance("parsnip", 4));

        Assert.Throws<InvalidOperationException>(() => plot.Harvest());
    }

    [Fact]
    public void Harvest_ThrowsWhenCropIsNull()
    {
        var plot = new PlotState(0, 0, IsTilled: true, IsLocked: false, IsWateredToday: false, IsHarvestReady: true, Crop: null);

        Assert.Throws<InvalidOperationException>(() => plot.Harvest());
    }
}
