using Godot;
using HarvestManor.World;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class CropVisualThemeTests
{
    [Fact]
    public void GetThemeColor_ReturnsKnownColorForRegisteredCrop()
    {
        var melon = CropVisualTheme.GetThemeColor("melon");
        var tomato = CropVisualTheme.GetThemeColor("tomato");

        Assert.NotEqual(melon, tomato);
    }

    [Fact]
    public void GetThemeColor_ReturnsFallbackForUnknownCrop()
    {
        var unknown = CropVisualTheme.GetThemeColor("not_a_real_crop");
        var fallback = CropVisualTheme.GetThemeColor(string.Empty);

        Assert.Equal(fallback, unknown);
    }

    [Fact]
    public void GetStageVisual_ReturnsIncreasingRadiusAcrossStages()
    {
        const string cropId = "melon";
        const int stageCount = 3;

        var sprout = CropVisualTheme.GetStageVisual(cropId, 0, stageCount, isReady: false);
        var mid = CropVisualTheme.GetStageVisual(cropId, 1, stageCount, isReady: false);
        var preMature = CropVisualTheme.GetStageVisual(cropId, 2, stageCount, isReady: false);
        var ready = CropVisualTheme.GetStageVisual(cropId, 2, stageCount, isReady: true);

        Assert.True(sprout.Radius < mid.Radius);
        Assert.True(mid.Radius < preMature.Radius);
        Assert.True(preMature.Radius < ready.Radius);
    }

    [Fact]
    public void GetStageVisual_ReadyUsesCropThemeColor()
    {
        var ready = CropVisualTheme.GetStageVisual("tomato", 2, 3, isReady: true);
        var theme = CropVisualTheme.GetThemeColor("tomato");

        Assert.Equal(theme, ready.FillColor);
    }

    [Fact]
    public void GetStageVisual_StagesProduceDistinctColors()
    {
        var sprout = CropVisualTheme.GetStageVisual("melon", 0, 3, isReady: false);
        var mid = CropVisualTheme.GetStageVisual("melon", 1, 3, isReady: false);
        var preMature = CropVisualTheme.GetStageVisual("melon", 2, 3, isReady: false);
        var ready = CropVisualTheme.GetStageVisual("melon", 2, 3, isReady: true);

        Assert.NotEqual(sprout.FillColor, mid.FillColor);
        Assert.NotEqual(mid.FillColor, preMature.FillColor);
        Assert.NotEqual(preMature.FillColor, ready.FillColor);
    }

    [Fact]
    public void GetStageVisual_HandlesSingleStageCropGracefully()
    {
        var visual = CropVisualTheme.GetStageVisual("melon", 0, stageCount: 1, isReady: false);

        Assert.True(visual.Radius > 0);
        Assert.True(visual.Sides >= 3);
    }

    [Fact]
    public void BuildShape_ReturnsExpectedVertexCountAndRadius()
    {
        var verts = CropVisualTheme.BuildShape(sides: 8, radius: 10f);

        Assert.Equal(8, verts.Length);
        foreach (var v in verts)
        {
            Assert.InRange(v.Length(), 9.99f, 10.01f);
        }
    }

    [Fact]
    public void BuildShape_ClampsSidesToMinimumTriangle()
    {
        var verts = CropVisualTheme.BuildShape(sides: 1, radius: 5f);

        Assert.Equal(3, verts.Length);
    }
}
