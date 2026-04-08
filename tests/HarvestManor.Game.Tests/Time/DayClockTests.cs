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
}
