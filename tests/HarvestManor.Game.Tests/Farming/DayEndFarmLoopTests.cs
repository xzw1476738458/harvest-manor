using HarvestManor.Core.Content;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Time;
using HarvestManor.World;
using Xunit;

namespace HarvestManor.Game.Tests.Farming;

public sealed class DayEndFarmLoopTests
{
    [Fact]
    public void ProcessDayEnd_AdvancesClock_GrowsWateredPlots_AndRestoresStamina()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            Season.Spring,
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        var stamina = new StaminaState(maximum: 100, current: 100);
        var farmGrid = new FarmGrid(2, 2);

        Assert.True(stamina.TrySpend(40));
        farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant(crop.Id).Water());

        var crops = new Dictionary<string, CropDefinition> { [crop.Id] = crop };
        var result = GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid, crops);
        var nextPlot = farmGrid.GetPlot(0, 0);

        Assert.True(result.DayRolled);
        Assert.False(result.SeasonChanged);
        Assert.Equal(new GameDate(Season.Spring, 2), clock.Date);
        Assert.Equal(6 * 60, clock.CurrentMinuteOfDay);
        Assert.Equal(1, nextPlot.Crop!.DaysGrown);
        Assert.False(nextPlot.IsWateredToday);
        Assert.Equal(stamina.Maximum, stamina.Current);
    }

    [Fact]
    public void ProcessDayEnd_DoesNotAdvanceFarmStateWhenDayDoesNotRoll()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            Season.Spring,
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        var stamina = new StaminaState(maximum: 100, current: 100);
        var farmGrid = new FarmGrid(2, 2);

        Assert.True(stamina.TrySpend(40));
        farmGrid.SetPlot(PlotState.Tilled(0, 0).Plant(crop.Id).Water());

        var crops = new Dictionary<string, CropDefinition> { [crop.Id] = crop };
        var result = GameBootstrap.ProcessDayEnd(clock, stamina, growth, farmGrid, crops, minutesToAdvance: 10);
        var sameDayPlot = farmGrid.GetPlot(0, 0);

        Assert.False(result.DayRolled);
        Assert.Equal(new GameDate(Season.Spring, 1), clock.Date);
        Assert.Equal((6 * 60) + 10, clock.CurrentMinuteOfDay);
        Assert.Equal(0, sameDayPlot.Crop!.DaysGrown);
        Assert.True(sameDayPlot.IsWateredToday);
        Assert.Equal(60, stamina.Current);
    }
}
