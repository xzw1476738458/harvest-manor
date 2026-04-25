using Xunit;

namespace HarvestManor.Game.Tests.UI;

public sealed class UiSceneLayoutTests
{
    [Fact]
    public void HudScene_AddsClockAndHintLabels()
    {
        var sceneContents = File.ReadAllText(FindScenePath("Hud.tscn"));

        Assert.Contains("[node name=\"ClockLabel\" type=\"Label\" parent=\"TopBar/Margin/Content/StatColumn/ClockBadge\"]", sceneContents);
        Assert.Contains("[node name=\"HintLabel\" type=\"Label\" parent=\"TopBar/Margin/Content/HintColumn\"]", sceneContents);
    }

    [Fact]
    public void HudScene_SplitsStatsAndHintIntoSeparateColumns()
    {
        var sceneContents = File.ReadAllText(FindScenePath("Hud.tscn"));

        Assert.Contains("[node name=\"StatColumn\" type=\"HBoxContainer\" parent=\"TopBar/Margin/Content\"]", sceneContents);
        Assert.Contains("[node name=\"HintColumn\" type=\"HBoxContainer\" parent=\"TopBar/Margin/Content\"]", sceneContents);
    }

    [Fact]
    public void ShopAndStoragePanels_AddTitlesAndHelperCopy()
    {
        var inventoryContents = File.ReadAllText(FindScenePath("InventoryPanel.tscn"));
        var shopContents = File.ReadAllText(FindScenePath("ShopPanel.tscn"));
        var storageContents = File.ReadAllText(FindScenePath("StoragePanel.tscn"));

        Assert.Contains("[node name=\"TitleLabel\" type=\"Label\" parent=\"Panel/Rows\"]", inventoryContents);
        Assert.Contains("[node name=\"HintLabel\" type=\"Label\" parent=\"Panel/Rows\"]", inventoryContents);
        Assert.Contains("[node name=\"TitleLabel\" type=\"Label\" parent=\"Panel/Rows\"]", shopContents);
        Assert.Contains("[node name=\"HintLabel\" type=\"Label\" parent=\"Panel/Rows\"]", shopContents);
        Assert.Contains("[node name=\"TitleLabel\" type=\"Label\" parent=\"Panel/Rows\"]", storageContents);
        Assert.Contains("[node name=\"HintLabel\" type=\"Label\" parent=\"Panel/Rows\"]", storageContents);
    }

    private static string FindScenePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "game", "scenes", "ui", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName} from test output.");
    }
}
