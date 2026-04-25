using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class PlayerSceneLayoutTests
{
    [Fact]
    public void PlayerScene_AttachesACamera2DSoTheFarmCanScrollWithThePlayer()
    {
        var sceneContents = File.ReadAllText(FindPlayerScenePath());

        Assert.Contains("[node name=\"Camera\" type=\"Camera2D\" parent=\".\"]", sceneContents);
    }

    [Fact]
    public void PlayerScene_EnablesCameraSmoothingForReadableMotion()
    {
        var sceneContents = File.ReadAllText(FindPlayerScenePath());

        Assert.Contains("position_smoothing_enabled = true", sceneContents);
    }

    private static string FindPlayerScenePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "game", "scenes", "world", "Player.tscn");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate Player.tscn from test output.");
    }
}
