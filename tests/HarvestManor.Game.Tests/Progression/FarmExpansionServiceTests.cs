using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class FarmExpansionServiceTests
{
    [Fact]
    public void UnlockPlot_AddsCoordinateKeyWhenGoldRequirementMet()
    {
        var unlocks = new UnlockState(new HashSet<string>());
        var expansion = new FarmExpansionService();

        var success = expansion.TryUnlockPlot(unlocks, "4,2", requiredGold: 120, currentGold: 200, out var updatedGold);

        Assert.True(success);
        Assert.Contains("4,2", unlocks.UnlockedPlotKeys);
        Assert.Equal(80, updatedGold);
    }

    [Fact]
    public void TryUnlockPlot_ReturnsFalseWhenPlotAlreadyUnlocked()
    {
        var unlocks = new UnlockState(new HashSet<string> { "4,2" });
        var expansion = new FarmExpansionService();

        var success = expansion.TryUnlockPlot(unlocks, "4,2", requiredGold: 120, currentGold: 200, out var updatedGold);

        Assert.False(success);
        Assert.Equal(200, updatedGold);
        Assert.Single(unlocks.UnlockedPlotKeys);
    }
}
