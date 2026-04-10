using Godot;
using HarvestManor.Core.Farming;
using HarvestManor.World;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class FarmPlotNodeTests
{
    [Fact]
    public void ResolveVisualState_UsesPlayerFacingCopyForCommonPlotStates()
    {
        var locked = FarmPlotNode.ResolveVisualState(
            new PlotState(2, 0, false, true, false, false, null),
            cropDisplayName: null,
            lockedHint: "Click: unlock (120g)");
        var untilled = FarmPlotNode.ResolveVisualState(PlotState.Wild(0, 0), cropDisplayName: null);
        var emptyTilled = FarmPlotNode.ResolveVisualState(PlotState.Tilled(0, 0), cropDisplayName: null);
        var harvestReady = FarmPlotNode.ResolveVisualState(
            new PlotState(0, 0, true, false, false, true, new CropInstance("parsnip", 4)),
            cropDisplayName: "Parsnip");

        Assert.Equal("New Plot\nClick: unlock (120g)", locked.LabelText);
        Assert.Equal("Open Plot\nClick: till", untilled.LabelText);
        Assert.Equal("Open Plot\nClick: plant", emptyTilled.LabelText);
        Assert.Equal("Parsnip\nReady: harvest", harvestReady.LabelText);
    }

    [Fact]
    public void ResolveVisualState_UsesDistinctFillColorsForMajorPlotStates()
    {
        var locked = FarmPlotNode.ResolveVisualState(
            new PlotState(2, 0, false, true, false, false, null),
            cropDisplayName: null);
        var untilled = FarmPlotNode.ResolveVisualState(PlotState.Wild(0, 0), cropDisplayName: null);
        var watered = FarmPlotNode.ResolveVisualState(
            new PlotState(0, 0, true, false, true, false, new CropInstance("parsnip", 2)),
            cropDisplayName: "Parsnip");
        var harvestReady = FarmPlotNode.ResolveVisualState(
            new PlotState(0, 0, true, false, false, true, new CropInstance("parsnip", 4)),
            cropDisplayName: "Parsnip");

        Assert.Equal(new Color(0.45f, 0.42f, 0.40f, 0.95f), locked.FillColor);
        Assert.Equal(new Color(0.42f, 0.62f, 0.31f, 0.95f), untilled.FillColor);
        Assert.Equal(new Color(0.34f, 0.56f, 0.72f, 0.95f), watered.FillColor);
        Assert.Equal(new Color(0.54f, 0.74f, 0.34f, 0.95f), harvestReady.FillColor);
    }
}
