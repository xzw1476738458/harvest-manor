using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class GatheringSceneLayoutTests
{
    [Fact]
    public void GatheringScene_DeclaresFourTreesAndThreeRocks()
    {
        var contents = File.ReadAllText(FindScenePath("GatheringScene.tscn"));

        for (var i = 1; i <= 4; i++)
        {
            Assert.Contains($"NodeId = \"forest_tree_{i}\"", contents);
        }

        for (var i = 1; i <= 3; i++)
        {
            Assert.Contains($"NodeId = \"quarry_rock_{i}\"", contents);
        }

        // Trees emit wood, rocks emit stone
        Assert.Contains("ItemId = \"wood\"", contents);
        Assert.Contains("ItemId = \"stone\"", contents);
    }

    [Fact]
    public void GatheringScene_HasExitGateBackToTown()
    {
        var contents = File.ReadAllText(FindScenePath("GatheringScene.tscn"));

        Assert.Contains("[node name=\"ExitGate\" type=\"Area2D\" parent=\".\"]", contents);
        Assert.Contains("TargetScene = \"town\"", contents);
    }

    [Fact]
    public void GatheringScene_HasTitleBadgeWithExpectedCopy()
    {
        var contents = File.ReadAllText(FindScenePath("GatheringScene.tscn"));

        Assert.Contains("text = \"Whispering Woods\"", contents);
    }

    [Fact]
    public void TownScene_OpensGateNorthIntoGatheringArea()
    {
        var contents = File.ReadAllText(FindScenePath("TownScene.tscn"));

        Assert.Contains("[node name=\"GateNorth\" type=\"Area2D\" parent=\".\"]", contents);
        Assert.Contains("TargetScene = \"gathering\"", contents);
    }

    private static string FindScenePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "game", "scenes", "world", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName} from test output.");
    }
}
