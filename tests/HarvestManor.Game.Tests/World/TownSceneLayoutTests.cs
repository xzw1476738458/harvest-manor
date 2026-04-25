using System.Text.RegularExpressions;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class TownSceneLayoutTests
{
    [Fact]
    public void TownScene_AddsVisibleHotspotsForAllServiceInteractions()
    {
        var sceneContents = File.ReadAllText(FindTownScenePath());

        Assert.Contains("[node name=\"HotspotVisual\" type=\"Polygon2D\" parent=\"Shop\"]", sceneContents);
        Assert.Contains("[node name=\"HotspotVisual\" type=\"Polygon2D\" parent=\"Storage\"]", sceneContents);
        Assert.Contains("[node name=\"HotspotVisual\" type=\"Polygon2D\" parent=\"RequestBoard\"]", sceneContents);
    }

    [Fact]
    public void TownScene_AddsBackdropLayersForMarketAtmosphere()
    {
        var sceneContents = File.ReadAllText(FindTownScenePath());

        Assert.Contains("[node name=\"SkyBackdrop\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"DirtBase\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"GrassMain\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"ServicePath\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
    }

    [Fact]
    public void TownScene_UsesDedicatedRequestPanelAndFullHeightServiceLane()
    {
        var sceneContents = File.ReadAllText(FindTownScenePath());
        var (maxX, maxY) = ExtractMaxPolygonCoordinate(sceneContents);

        Assert.Contains("[node name=\"RequestStatusPanel\" type=\"PanelContainer\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"RequestStatusLabel\" type=\"Label\" parent=\"RequestStatusPanel/Margin/Content\"]", sceneContents);
        Assert.True(maxX >= 1250, $"Expected town scene framing to reach at least x=1250, but max polygon x was {maxX}.");
        Assert.True(maxY >= 700, $"Expected town scene framing to reach at least y=700, but max polygon y was {maxY}.");
    }

    private static string FindTownScenePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "game", "scenes", "world", "TownScene.tscn");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate TownScene.tscn from test output.");
    }

    private static (float MaxX, float MaxY) ExtractMaxPolygonCoordinate(string sceneContents)
    {
        var matches = Regex.Matches(
            sceneContents,
            @"polygon = PackedVector2Array\((?<coords>[^)]*)\)",
            RegexOptions.CultureInvariant);

        var maxX = 0f;
        var maxY = 0f;

        foreach (Match match in matches)
        {
            var values = match.Groups["coords"].Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(value => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();

            for (var index = 0; index + 1 < values.Length; index += 2)
            {
                maxX = Math.Max(maxX, values[index]);
                maxY = Math.Max(maxY, values[index + 1]);
            }
        }

        return (maxX, maxY);
    }
}
