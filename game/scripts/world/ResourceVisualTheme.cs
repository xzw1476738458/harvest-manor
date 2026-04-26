using Godot;

namespace HarvestManor.World;

public static class ResourceVisualTheme
{
    public readonly record struct ResourceVisual(Vector2[] Polygon, Color FillColor);

    public static ResourceVisual Resolve(string itemId) => itemId switch
    {
        "wood" => BuildTreeVisual(),
        "stone" => BuildRockVisual(),
        _ => BuildFallbackVisual()
    };

    private static ResourceVisual BuildTreeVisual()
    {
        // A pine-like silhouette: a triangular canopy stacked over a trunk, in one polygon
        var polygon = new[]
        {
            new Vector2(-6f, 28f),
            new Vector2(6f, 28f),
            new Vector2(6f, 0f),
            new Vector2(28f, 0f),
            new Vector2(14f, -22f),
            new Vector2(20f, -22f),
            new Vector2(0f, -48f),
            new Vector2(-20f, -22f),
            new Vector2(-14f, -22f),
            new Vector2(-28f, 0f),
            new Vector2(-6f, 0f),
        };
        return new ResourceVisual(polygon, new Color(0.20f, 0.48f, 0.24f, 1f));
    }

    private static ResourceVisual BuildRockVisual()
    {
        // A chunky hexagonal boulder
        var polygon = new[]
        {
            new Vector2(-22f, 4f),
            new Vector2(-12f, -18f),
            new Vector2(12f, -22f),
            new Vector2(24f, -2f),
            new Vector2(18f, 18f),
            new Vector2(-14f, 20f),
        };
        return new ResourceVisual(polygon, new Color(0.55f, 0.55f, 0.58f, 1f));
    }

    private static ResourceVisual BuildFallbackVisual()
    {
        var polygon = new[]
        {
            new Vector2(-16f, -16f),
            new Vector2(16f, -16f),
            new Vector2(16f, 16f),
            new Vector2(-16f, 16f),
        };
        return new ResourceVisual(polygon, new Color(0.7f, 0.5f, 0.3f, 1f));
    }
}
