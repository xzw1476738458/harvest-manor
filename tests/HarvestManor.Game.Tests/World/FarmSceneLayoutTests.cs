using System.Text.RegularExpressions;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class FarmSceneLayoutTests
{
    [Fact]
    public void FarmScene_ExposesDefaultUnlockedPlotsAndExpansionDemoPlot()
    {
        var sceneContents = File.ReadAllText(FindFarmScenePath());
        var plotCoordinates = ParsePlotCoordinates(sceneContents);

        Assert.Contains((0, 0), plotCoordinates);
        Assert.Contains((1, 0), plotCoordinates);
        Assert.Contains((0, 1), plotCoordinates);
        Assert.Contains((1, 1), plotCoordinates);
        Assert.Contains((2, 0), plotCoordinates);
    }

    [Fact]
    public void FarmScene_AddsVisibleHotspotsForBedAndPlots()
    {
        var sceneContents = File.ReadAllText(FindFarmScenePath());

        Assert.Contains("[node name=\"HotspotVisual\" type=\"Polygon2D\" parent=\"Bed\"]", sceneContents);
        Assert.Contains("[node name=\"PlotVisual\" type=\"Polygon2D\" parent=\"Plot00\"]", sceneContents);
        Assert.Contains("[node name=\"PlotVisual\" type=\"Polygon2D\" parent=\"Plot10\"]", sceneContents);
        Assert.Contains("[node name=\"PlotVisual\" type=\"Polygon2D\" parent=\"Plot20\"]", sceneContents);
        Assert.Contains("[node name=\"PlotVisual\" type=\"Polygon2D\" parent=\"Plot01\"]", sceneContents);
        Assert.Contains("[node name=\"PlotVisual\" type=\"Polygon2D\" parent=\"Plot11\"]", sceneContents);
    }

    [Fact]
    public void FarmScene_AddsBackdropLayersForFieldAtmosphere()
    {
        var sceneContents = File.ReadAllText(FindFarmScenePath());

        Assert.Contains("[node name=\"SkyBackdrop\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"FieldBackdrop\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"PathBackdrop\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"PorchBackdrop\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
    }

    [Fact]
    public void FarmScene_UsesDedicatedStatusPanelAndWideSceneFrame()
    {
        var sceneContents = File.ReadAllText(FindFarmScenePath());
        var (maxX, maxY) = ExtractMaxPolygonCoordinate(sceneContents);

        Assert.Contains("[node name=\"FarmStatusPanel\" type=\"PanelContainer\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"FarmStatusLabel\" type=\"Label\" parent=\"FarmStatusPanel/Margin/Content\"]", sceneContents);
        Assert.True(maxX >= 820, $"Expected farm scene framing to reach at least x=820, but max polygon x was {maxX}.");
        Assert.True(maxY >= 700, $"Expected farm scene framing to reach at least y=700, but max polygon y was {maxY}.");
    }

    private static string FindFarmScenePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "game", "scenes", "world", "FarmScene.tscn");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate FarmScene.tscn from test output.");
    }

    private static HashSet<(int X, int Y)> ParsePlotCoordinates(string sceneContents)
    {
        var matches = Regex.Matches(
            sceneContents,
            @"GridX = (?<x>\d+)\r?\nGridY = (?<y>\d+)",
            RegexOptions.CultureInvariant);

        return matches
            .Select(match => (
                X: int.Parse(match.Groups["x"].Value),
                Y: int.Parse(match.Groups["y"].Value)))
            .ToHashSet();
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
