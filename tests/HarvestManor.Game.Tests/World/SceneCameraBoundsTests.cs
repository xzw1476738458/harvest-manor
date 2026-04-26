using System;
using System.IO;
using Xunit;

namespace HarvestManor.Game.Tests.World;

public sealed class SceneCameraBoundsTests
{
    [Theory]
    [InlineData("FarmScene.tscn", 24, 104, 1256, 996)]
    [InlineData("TownScene.tscn", 24, 104, 1256, 696)]
    [InlineData("CottageInterior.tscn", 24, 104, 1256, 696)]
    [InlineData("ShopInterior.tscn", 24, 104, 1256, 696)]
    [InlineData("BarnInterior.tscn", 24, 104, 1256, 696)]
    public void Scene_DeclaresCameraBoundsAndCoversItsContent(string sceneFile, int left, int top, int right, int bottom)
    {
        var path = FindScenePath(sceneFile);
        var contents = File.ReadAllText(path);

        Assert.Contains("path=\"res://scripts/world/CameraBounds.cs\"", contents);
        Assert.Contains("[node name=\"CameraBounds\" type=\"Node\" parent=\".\"]", contents);
        Assert.Contains($"Left = {left}", contents);
        Assert.Contains($"Top = {top}", contents);
        Assert.Contains($"Right = {right}", contents);
        Assert.Contains($"Bottom = {bottom}", contents);
    }

    private static string FindScenePath(string sceneFile)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "game", "scenes", "world", sceneFile);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {sceneFile} relative to the test base directory.");
    }
}
