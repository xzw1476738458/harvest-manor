using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class ExpansionTierServiceTests
{
    [Fact]
    public void DefaultUnlockedPlotKeys_AreTheRingZeroCornerBlock()
    {
        var service = ExpansionTierService.CreateDefault();

        Assert.Equal(
            new[] { "0,0", "0,1", "1,0", "1,1" }.OrderBy(static k => k),
            service.DefaultUnlockedPlotKeys.OrderBy(static k => k));
    }

    [Theory]
    [InlineData(0, 0, null)] // ring 0 is free, has no unlock cost
    [InlineData(1, 1, null)]
    [InlineData(2, 0, 120)]  // ring 1 keeps the milestone-1 demo price
    [InlineData(0, 2, 120)]
    [InlineData(2, 2, 120)]
    [InlineData(3, 0, 280)]  // ring 2 starts to scale
    [InlineData(3, 3, 280)]
    [InlineData(0, 3, 280)]
    [InlineData(4, 0, 600)]  // ring 3 is a meaningful goldsink
    [InlineData(4, 4, 600)]
    [InlineData(2, 4, 600)]
    [InlineData(5, 0, 1200)] // ring 4 caps out the late-day cost
    [InlineData(5, 5, 1200)]
    [InlineData(0, 5, 1200)]
    public void GetUnlockCost_ReturnsRingPriceForKnownPlots(int x, int y, int? expectedCost)
    {
        var service = ExpansionTierService.CreateDefault();

        var cost = service.GetUnlockCost(x, y);

        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void GetUnlockCost_ReturnsNullWhenPlotIsOutsideTheGrid()
    {
        var service = ExpansionTierService.CreateDefault();

        Assert.Null(service.GetUnlockCost(6, 0));
        Assert.Null(service.GetUnlockCost(0, 6));
        Assert.Null(service.GetUnlockCost(-1, 0));
        Assert.Null(service.GetUnlockCost(0, -1));
    }

    [Theory]
    [InlineData("2,0", 120)]
    [InlineData("3,3", 280)]
    [InlineData("4,4", 600)]
    [InlineData("5,5", 1200)]
    [InlineData("0,0", null)]
    [InlineData("not-a-key", null)]
    [InlineData("9,9", null)]
    public void GetUnlockCostForKey_DelegatesToCoordinateLookup(string plotKey, int? expectedCost)
    {
        var service = ExpansionTierService.CreateDefault();

        Assert.Equal(expectedCost, service.GetUnlockCost(plotKey));
    }

    [Fact]
    public void EnumerateLockedTiers_ReturnsAllRingsInIncreasingCostOrder()
    {
        var service = ExpansionTierService.CreateDefault();

        var costsInOrder = service.EnumerateLockedTiers()
            .Select(static tier => tier.UnlockCost)
            .ToList();

        Assert.Equal(new[] { 120, 280, 600, 1200 }, costsInOrder);
    }

    [Fact]
    public void GetActiveTier_ReturnsRingOneForAFreshSave()
    {
        var service = ExpansionTierService.CreateDefault();
        var unlockState = new UnlockState(new HashSet<string>(service.DefaultUnlockedPlotKeys));

        var tier = service.GetActiveTier(unlockState);

        Assert.NotNull(tier);
        Assert.Equal(120, tier!.UnlockCost);
        Assert.Contains("2,0", tier.PlotKeys);
        Assert.Contains("0,2", tier.PlotKeys);
        Assert.Contains("2,2", tier.PlotKeys);
    }

    [Fact]
    public void GetActiveTier_AdvancesToTheNextRingWhenAllOfTheCurrentRingIsUnlocked()
    {
        var service = ExpansionTierService.CreateDefault();
        var keys = new HashSet<string>(service.DefaultUnlockedPlotKeys);
        var firstLockedRing = service.EnumerateLockedTiers()[0];
        foreach (var key in firstLockedRing.PlotKeys)
        {
            keys.Add(key);
        }
        var unlockState = new UnlockState(keys);

        var tier = service.GetActiveTier(unlockState);

        Assert.NotNull(tier);
        Assert.Equal(280, tier!.UnlockCost);
    }

    [Fact]
    public void GetActiveTier_StaysOnTheCurrentRingWhenSomePlotsAreStillLockedThere()
    {
        var service = ExpansionTierService.CreateDefault();
        var keys = new HashSet<string>(service.DefaultUnlockedPlotKeys) { "2,0" };
        var unlockState = new UnlockState(keys);

        var tier = service.GetActiveTier(unlockState);

        Assert.NotNull(tier);
        Assert.Equal(120, tier!.UnlockCost);
    }

    [Fact]
    public void GetActiveTier_ReturnsNullWhenEverythingIsUnlocked()
    {
        var service = ExpansionTierService.CreateDefault();
        var keys = new HashSet<string>(service.DefaultUnlockedPlotKeys);
        foreach (var tier in service.EnumerateLockedTiers())
        {
            foreach (var key in tier.PlotKeys)
            {
                keys.Add(key);
            }
        }
        var unlockState = new UnlockState(keys);

        Assert.Null(service.GetActiveTier(unlockState));
    }
}
