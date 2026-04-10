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
        Assert.Contains("[node name=\"TownBackdrop\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"ServicePath\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
        Assert.Contains("[node name=\"BannerStrip\" type=\"Polygon2D\" parent=\".\"]", sceneContents);
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
}
