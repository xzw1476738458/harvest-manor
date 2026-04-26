using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Time;
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

    [Fact]
    public void ResolveVisualState_HidesHintAndDarkensFillForLockedPlotsOutsideTheActiveTier()
    {
        var distantLocked = FarmPlotNode.ResolveVisualState(
            new PlotState(5, 5, false, true, false, false, null),
            cropDisplayName: null,
            lockedHint: "Click: unlock (1200g)",
            isInActiveTier: false);

        var activeLocked = FarmPlotNode.ResolveVisualState(
            new PlotState(2, 0, false, true, false, false, null),
            cropDisplayName: null,
            lockedHint: "Click: unlock (120g)",
            isInActiveTier: true);

        Assert.Equal(string.Empty, distantLocked.LabelText);
        Assert.NotEqual(activeLocked.FillColor, distantLocked.FillColor);
        Assert.True(
            distantLocked.FillColor.R < activeLocked.FillColor.R
                && distantLocked.FillColor.G < activeLocked.FillColor.G
                && distantLocked.FillColor.B < activeLocked.FillColor.B,
            "Distant locked plots should render with a darker fill than the active tier.");
    }

    [Fact]
    public void ResolveVisualState_LockedDefaultsToActiveTierAppearanceForBackwardsCompatibility()
    {
        var implicitlyActive = FarmPlotNode.ResolveVisualState(
            new PlotState(2, 0, false, true, false, false, null),
            cropDisplayName: null,
            lockedHint: "Click: unlock (120g)");

        Assert.Equal("New Plot\nClick: unlock (120g)", implicitlyActive.LabelText);
        Assert.Equal(new Color(0.45f, 0.42f, 0.40f, 0.95f), implicitlyActive.FillColor);
    }

    [Fact]
    public void ResolveCropSpriteVisual_HiddenWhenPlotIsLockedOrUntilledOrEmpty()
    {
        var melon = BuildMelonDefinition();

        var locked = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, false, true, false, false, null), melon);
        var untilled = FarmPlotNode.ResolveCropSpriteVisual(PlotState.Wild(0, 0), melon);
        var emptyTilled = FarmPlotNode.ResolveCropSpriteVisual(PlotState.Tilled(0, 0), melon);
        var noDefinition = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, true, false, false, false, new CropInstance("melon", 0)),
            cropDefinition: null);

        Assert.False(locked.Visible);
        Assert.False(untilled.Visible);
        Assert.False(emptyTilled.Visible);
        Assert.False(noDefinition.Visible);
    }

    [Fact]
    public void ResolveCropSpriteVisual_VisibleWithRisingRadiusAcrossGrowthDays()
    {
        var melon = BuildMelonDefinition();

        var sprout = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, true, false, true, false, new CropInstance("melon", 0)),
            melon);
        var mid = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, true, false, true, false, new CropInstance("melon", 4)),
            melon);
        var preMature = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, true, false, true, false, new CropInstance("melon", 8)),
            melon);
        var ready = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, true, false, false, true, new CropInstance("melon", 12)),
            melon);

        Assert.True(sprout.Visible && mid.Visible && preMature.Visible && ready.Visible);
        Assert.True(sprout.StageVisual.Radius < mid.StageVisual.Radius);
        Assert.True(mid.StageVisual.Radius < preMature.StageVisual.Radius);
        Assert.True(preMature.StageVisual.Radius < ready.StageVisual.Radius);
        Assert.NotEqual(sprout.StageVisual.FillColor, ready.StageVisual.FillColor);
    }

    [Fact]
    public void ResolveCropSpriteVisual_ReadyUsesCropThemeColor()
    {
        var melon = BuildMelonDefinition();
        var ready = FarmPlotNode.ResolveCropSpriteVisual(
            new PlotState(0, 0, true, false, false, true, new CropInstance("melon", 12)),
            melon);

        Assert.True(ready.Visible);
        Assert.Equal(CropVisualTheme.GetThemeColor("melon"), ready.StageVisual.FillColor);
    }

    private static CropDefinition BuildMelonDefinition() => new(
        Id: "melon",
        DisplayName: "Melon",
        Season: Season.Summer,
        SeedItemId: "melon_seed",
        HarvestItemId: "melon_crop",
        PurchasePrice: 80,
        SellPrice: 250,
        TotalGrowthDays: 12,
        GrowthStageDays: new[] { 3, 4, 5 });
}
