using Godot;

namespace HarvestManor.World;

public static class ResourceVisualTheme
{
    public readonly record struct VisualLayer(Vector2[] Polygon, Color FillColor, int ZOffset = 0);

    public readonly record struct ResourceVisual(VisualLayer[] Active, VisualLayer[] Harvested);

    public static ResourceVisual Resolve(string itemId) => itemId switch
    {
        "wood" => new ResourceVisual(BuildLivePine(), BuildPineStump()),
        "stone" => new ResourceVisual(BuildLiveBoulder(), BuildRubblePile()),
        _ => new ResourceVisual(BuildFallback(), BuildFallback()),
    };

    private static VisualLayer[] BuildLivePine()
    {
        // Stacked pine: ground shadow, trunk shadow + body, three canopy layers (dark to highlight)
        return new[]
        {
            new VisualLayer(
                new[]
                {
                    new Vector2(-26f, 30f),
                    new Vector2(26f, 30f),
                    new Vector2(20f, 38f),
                    new Vector2(-20f, 38f),
                },
                new Color(0f, 0f, 0f, 0.22f),
                ZOffset: -3),
            new VisualLayer(
                new[]
                {
                    new Vector2(-9f, 30f),
                    new Vector2(9f, 30f),
                    new Vector2(7f, -10f),
                    new Vector2(-7f, -10f),
                },
                new Color(0.30f, 0.18f, 0.10f, 1f),
                ZOffset: -2),
            new VisualLayer(
                new[]
                {
                    new Vector2(-3f, 30f),
                    new Vector2(3f, 30f),
                    new Vector2(2f, -10f),
                    new Vector2(-3f, -10f),
                },
                new Color(0.45f, 0.30f, 0.18f, 1f),
                ZOffset: -1),
            new VisualLayer(
                new[]
                {
                    new Vector2(0f, -68f),
                    new Vector2(36f, -8f),
                    new Vector2(22f, -8f),
                    new Vector2(30f, 12f),
                    new Vector2(-30f, 12f),
                    new Vector2(-22f, -8f),
                    new Vector2(-36f, -8f),
                },
                new Color(0.10f, 0.30f, 0.14f, 1f),
                ZOffset: 0),
            new VisualLayer(
                new[]
                {
                    new Vector2(0f, -56f),
                    new Vector2(26f, -8f),
                    new Vector2(-26f, -8f),
                },
                new Color(0.18f, 0.45f, 0.22f, 1f),
                ZOffset: 1),
            new VisualLayer(
                new[]
                {
                    new Vector2(0f, -42f),
                    new Vector2(14f, -16f),
                    new Vector2(-14f, -16f),
                },
                new Color(0.34f, 0.62f, 0.34f, 1f),
                ZOffset: 2),
        };
    }

    private static VisualLayer[] BuildPineStump()
    {
        // Short stump + ringed top + tiny shadow, conveys 'just chopped'
        return new[]
        {
            new VisualLayer(
                new[]
                {
                    new Vector2(-18f, 26f),
                    new Vector2(18f, 26f),
                    new Vector2(14f, 32f),
                    new Vector2(-14f, 32f),
                },
                new Color(0f, 0f, 0f, 0.22f),
                ZOffset: -3),
            new VisualLayer(
                new[]
                {
                    new Vector2(-12f, 26f),
                    new Vector2(12f, 26f),
                    new Vector2(11f, 6f),
                    new Vector2(-11f, 6f),
                },
                new Color(0.32f, 0.20f, 0.12f, 1f),
                ZOffset: -2),
            new VisualLayer(
                new[]
                {
                    new Vector2(-12f, 8f),
                    new Vector2(-6f, 2f),
                    new Vector2(6f, 2f),
                    new Vector2(12f, 8f),
                    new Vector2(6f, 14f),
                    new Vector2(-6f, 14f),
                },
                new Color(0.55f, 0.36f, 0.22f, 1f),
                ZOffset: -1),
            new VisualLayer(
                new[]
                {
                    new Vector2(-4f, 8f),
                    new Vector2(-2f, 6f),
                    new Vector2(2f, 6f),
                    new Vector2(4f, 8f),
                    new Vector2(2f, 10f),
                    new Vector2(-2f, 10f),
                },
                new Color(0.40f, 0.24f, 0.14f, 1f),
                ZOffset: 0),
        };
    }

    private static VisualLayer[] BuildLiveBoulder()
    {
        // Boulder: shadow, body, highlight wedge, mossy patch
        return new[]
        {
            new VisualLayer(
                new[]
                {
                    new Vector2(-28f, 22f),
                    new Vector2(28f, 22f),
                    new Vector2(22f, 30f),
                    new Vector2(-22f, 30f),
                },
                new Color(0f, 0f, 0f, 0.22f),
                ZOffset: -3),
            new VisualLayer(
                new[]
                {
                    new Vector2(-26f, 6f),
                    new Vector2(-14f, -20f),
                    new Vector2(10f, -24f),
                    new Vector2(26f, -6f),
                    new Vector2(22f, 18f),
                    new Vector2(-18f, 22f),
                },
                new Color(0.46f, 0.46f, 0.50f, 1f),
                ZOffset: -2),
            new VisualLayer(
                new[]
                {
                    new Vector2(-18f, -8f),
                    new Vector2(-8f, -18f),
                    new Vector2(6f, -20f),
                    new Vector2(2f, -10f),
                    new Vector2(-10f, -2f),
                },
                new Color(0.68f, 0.68f, 0.72f, 1f),
                ZOffset: -1),
            new VisualLayer(
                new[]
                {
                    new Vector2(8f, -16f),
                    new Vector2(20f, -10f),
                    new Vector2(22f, 0f),
                    new Vector2(14f, -2f),
                },
                new Color(0.34f, 0.55f, 0.32f, 1f),
                ZOffset: 0),
            new VisualLayer(
                new[]
                {
                    new Vector2(-12f, 14f),
                    new Vector2(-2f, 10f),
                    new Vector2(4f, 16f),
                    new Vector2(-6f, 20f),
                },
                new Color(0.30f, 0.50f, 0.30f, 1f),
                ZOffset: 1),
        };
    }

    private static VisualLayer[] BuildRubblePile()
    {
        // Three small rocks scattered + shadow, conveys 'just mined'
        return new[]
        {
            new VisualLayer(
                new[]
                {
                    new Vector2(-22f, 16f),
                    new Vector2(22f, 16f),
                    new Vector2(18f, 24f),
                    new Vector2(-18f, 24f),
                },
                new Color(0f, 0f, 0f, 0.22f),
                ZOffset: -3),
            new VisualLayer(
                new[]
                {
                    new Vector2(-18f, 10f),
                    new Vector2(-12f, 2f),
                    new Vector2(-4f, 4f),
                    new Vector2(-2f, 14f),
                    new Vector2(-14f, 16f),
                },
                new Color(0.50f, 0.50f, 0.54f, 1f),
                ZOffset: -2),
            new VisualLayer(
                new[]
                {
                    new Vector2(2f, 8f),
                    new Vector2(10f, 0f),
                    new Vector2(18f, 6f),
                    new Vector2(14f, 14f),
                    new Vector2(4f, 14f),
                },
                new Color(0.46f, 0.46f, 0.50f, 1f),
                ZOffset: -1),
            new VisualLayer(
                new[]
                {
                    new Vector2(-6f, 14f),
                    new Vector2(2f, 10f),
                    new Vector2(6f, 16f),
                    new Vector2(-2f, 18f),
                },
                new Color(0.58f, 0.58f, 0.62f, 1f),
                ZOffset: 0),
        };
    }

    private static VisualLayer[] BuildFallback()
    {
        return new[]
        {
            new VisualLayer(
                new[]
                {
                    new Vector2(-16f, -16f),
                    new Vector2(16f, -16f),
                    new Vector2(16f, 16f),
                    new Vector2(-16f, 16f),
                },
                new Color(0.7f, 0.5f, 0.3f, 1f)),
        };
    }
}
