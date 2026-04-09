using Godot;
using HarvestManor.World;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class InteractionHoverStyleTests
{
    [Fact]
    public void ResolveScale_SlightlyEnlargesHoveredInteractions()
    {
        Assert.Equal(Vector2.One, InteractionHoverStyle.ResolveScale(isHovered: false));
        Assert.Equal(new Vector2(1.05f, 1.05f), InteractionHoverStyle.ResolveScale(isHovered: true));
    }

    [Fact]
    public void ResolveColor_LightensHoveredHotspotsWithoutChangingAlpha()
    {
        var baseColor = new Color(0.45f, 0.55f, 0.35f, 0.82f);

        var idleColor = InteractionHoverStyle.ResolveColor(baseColor, isHovered: false);
        var hoveredColor = InteractionHoverStyle.ResolveColor(baseColor, isHovered: true);

        Assert.Equal(baseColor, idleColor);
        Assert.True(hoveredColor.R > baseColor.R);
        Assert.True(hoveredColor.G > baseColor.G);
        Assert.True(hoveredColor.B > baseColor.B);
        Assert.Equal(baseColor.A, hoveredColor.A);
    }
}
