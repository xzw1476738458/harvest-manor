using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Progression;
using HarvestManor.World;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class FarmExpansionServiceTests
{
    [Fact]
    public void UnlockPlot_AddsCoordinateKeyWhenGoldRequirementMet()
    {
        var unlocks = new UnlockState(new HashSet<string>());
        var expansion = new FarmExpansionService();
        var wallet = new Wallet(200);

        var success = expansion.TryUnlockPlot(unlocks, "4,2", requiredGold: 120, wallet);

        Assert.True(success);
        Assert.Contains("4,2", unlocks.UnlockedPlotKeys);
        Assert.Equal(80, wallet.Gold);
    }

    [Fact]
    public void TryUnlockPlot_ReturnsFalseWhenPlotAlreadyUnlocked()
    {
        var unlocks = new UnlockState(new HashSet<string> { "4,2" });
        var expansion = new FarmExpansionService();
        var wallet = new Wallet(200);

        var success = expansion.TryUnlockPlot(unlocks, "4,2", requiredGold: 120, wallet);

        Assert.False(success);
        Assert.Equal(200, wallet.Gold);
        Assert.Single(unlocks.UnlockedPlotKeys);
    }

    [Fact]
    public void SyncFarmGridLocksFromUnlockState_AppliesUnlockStateAsSourceOfTruth()
    {
        var farmGrid = new FarmGrid(2, 2);
        var unlocks = new UnlockState(new HashSet<string> { "0,0", "1,1" });

        GameBootstrap.SyncFarmGridLocksFromUnlockState(farmGrid, unlocks);

        Assert.False(farmGrid.GetPlot(0, 0).IsLocked);
        Assert.True(farmGrid.GetPlot(1, 0).IsLocked);
        Assert.True(farmGrid.GetPlot(0, 1).IsLocked);
        Assert.False(farmGrid.GetPlot(1, 1).IsLocked);
    }

    [Fact]
    public void CreatePlotSnapshots_DerivesLockedFlagFromUnlockState()
    {
        var farmGrid = new FarmGrid(2, 1);
        farmGrid.SetPlot(PlotState.Wild(0, 0) with { IsLocked = true });
        farmGrid.SetPlot(PlotState.Wild(1, 0) with { IsLocked = false });

        var unlocks = new UnlockState(new HashSet<string> { "0,0" });
        var snapshots = GameBootstrap.CreatePlotSnapshots(farmGrid, unlocks);

        var unlockedPlot = Assert.Single(snapshots, plot => plot.X == 0 && plot.Y == 0);
        var lockedPlot = Assert.Single(snapshots, plot => plot.X == 1 && plot.Y == 0);

        Assert.False(unlockedPlot.IsLocked);
        Assert.True(lockedPlot.IsLocked);
    }
}
