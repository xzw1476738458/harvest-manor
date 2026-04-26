using System;
using System.Collections.Generic;
using Godot;

namespace HarvestManor.World;

/// <summary>
/// Maps crop ids to per-stage visual settings (fill color, radius, vertex count) so the
/// farm plot can show a sprout -> mid -> pre-mature -> ready progression instead of a
/// flat colored square.
/// </summary>
public static class CropVisualTheme
{
    public readonly record struct StageVisual(Color FillColor, float Radius, int Sides);

    private const int SproutSides = 6;
    private const int MidSides = 8;
    private const int MatureSides = 10;
    private const int ReadySides = 12;

    private const float SproutRadius = 7f;
    private const float MidRadius = 13f;
    private const float MatureRadius = 18f;
    private const float ReadyRadius = 22f;

    private static readonly Color SproutColor = new(0.36f, 0.58f, 0.24f, 1f);
    private static readonly Color MidGreenColor = new(0.28f, 0.50f, 0.22f, 1f);
    private static readonly Color FallbackTheme = new(0.68f, 0.55f, 0.30f, 1f);

    private static readonly Dictionary<string, Color> ThemeColors = new(StringComparer.Ordinal)
    {
        ["parsnip"] = new Color(0.92f, 0.84f, 0.50f, 1f),
        ["potato"] = new Color(0.58f, 0.43f, 0.28f, 1f),
        ["melon"] = new Color(0.62f, 0.78f, 0.32f, 1f),
        ["tomato"] = new Color(0.84f, 0.32f, 0.26f, 1f),
        ["corn"] = new Color(0.92f, 0.78f, 0.30f, 1f),
        ["pumpkin"] = new Color(0.84f, 0.50f, 0.20f, 1f),
        ["yam"] = new Color(0.55f, 0.32f, 0.62f, 1f),
        ["cranberry"] = new Color(0.66f, 0.20f, 0.28f, 1f),
        ["winter_root"] = new Color(0.74f, 0.74f, 0.78f, 1f),
        ["crystal_fruit"] = new Color(0.55f, 0.78f, 0.86f, 1f),
    };

    public static Color GetThemeColor(string cropId)
        => string.IsNullOrEmpty(cropId)
            ? FallbackTheme
            : ThemeColors.TryGetValue(cropId, out var c) ? c : FallbackTheme;

    /// <summary>
    /// Resolve the visual that should be drawn on the plot for the given crop / stage.
    /// stageCount is the total stage count from the crop definition (typically 3).
    /// stageIndex is 0-based; isReady should reflect PlotState.IsHarvestReady.
    /// </summary>
    public static StageVisual GetStageVisual(string cropId, int stageIndex, int stageCount, bool isReady)
    {
        if (isReady)
        {
            return new StageVisual(GetThemeColor(cropId), ReadyRadius, ReadySides);
        }

        var clampedStage = Math.Clamp(stageIndex, 0, Math.Max(stageCount - 1, 0));
        var span = Math.Max(stageCount - 1, 1);
        var t = (float)clampedStage / span;

        if (clampedStage == 0)
        {
            return new StageVisual(SproutColor, SproutRadius, SproutSides);
        }

        // Final non-ready stage uses the crop theme but slightly desaturated by lerp.
        var isFinalNonReady = clampedStage >= stageCount - 1;
        if (isFinalNonReady)
        {
            var preMatureColor = MidGreenColor.Lerp(GetThemeColor(cropId), 0.65f);
            return new StageVisual(preMatureColor, MatureRadius, MatureSides);
        }

        // Middle stages: blend mid-green toward theme color proportionally.
        var blendWeight = Mathf.Clamp(t * 0.5f, 0f, 0.6f);
        var midColor = MidGreenColor.Lerp(GetThemeColor(cropId), blendWeight);
        return new StageVisual(midColor, MidRadius, MidSides);
    }

    /// <summary>
    /// Build a regular convex polygon centered at origin with the given side count and radius.
    /// Used by FarmPlotNode to render the crop sprite as a Polygon2D shape.
    /// </summary>
    public static Vector2[] BuildShape(int sides, float radius)
    {
        if (sides < 3)
        {
            sides = 3;
        }

        var verts = new Vector2[sides];
        const float startAngle = -Mathf.Pi / 2f;
        for (var i = 0; i < sides; i++)
        {
            var angle = startAngle + (float)(i * Math.PI * 2.0 / sides);
            verts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return verts;
    }
}
