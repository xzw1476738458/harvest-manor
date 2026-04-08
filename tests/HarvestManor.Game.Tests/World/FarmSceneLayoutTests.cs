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
}
