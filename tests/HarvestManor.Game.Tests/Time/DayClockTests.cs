using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Time;

public sealed class DayClockTests
{
    [Fact]
    public void AdvanceMinutes_RollsToNextDayAfterDayEnd()
    {
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);

        var rolled = clock.AdvanceMinutes(20 * 60);

        Assert.True(rolled);
        Assert.Equal(new GameDate(Season.Spring, 2), clock.Date);
        Assert.Equal(6 * 60, clock.CurrentMinuteOfDay);
    }

    [Fact]
    public void AdvanceMinutes_CarriesOverflowMinutesIntoNextDay()
    {
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);

        var rolled = clock.AdvanceMinutes((20 * 60) + 45);

        Assert.True(rolled);
        Assert.Equal(new GameDate(Season.Spring, 2), clock.Date);
        Assert.Equal((6 * 60) + 45, clock.CurrentMinuteOfDay);
    }

    [Fact]
    public void AdvanceMinutes_CarriesAcrossMultipleDayBoundaries()
    {
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);

        var rolled = clock.AdvanceMinutes((40 * 60) + 15);

        Assert.True(rolled);
        Assert.Equal(new GameDate(Season.Spring, 3), clock.Date);
        Assert.Equal((6 * 60) + 15, clock.CurrentMinuteOfDay);
    }

    [Fact]
    public void NextDay_RollsSeasonWhenDayReachesSeasonLength()
    {
        var date = new GameDate(Season.Spring, 28);

        var next = date.NextDay();

        Assert.Equal(new GameDate(Season.Summer, 1), next);
    }
}
