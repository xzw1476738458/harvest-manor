using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Time;

public sealed class TimeOfDayControllerTests
{
    [Theory]
    [InlineData(8 * 60 + 59, false)]
    [InlineData(9 * 60, true)]
    [InlineData(12 * 60, true)]
    [InlineData(17 * 60 + 59, true)]
    [InlineData(18 * 60, false)]
    [InlineData(20 * 60, false)]
    [InlineData(6 * 60, false)]
    public void IsShopOpen_ReturnsTrueOnlyWithinShopHours(int minute, bool expected)
    {
        Assert.Equal(expected, TimeOfDayController.IsShopOpen(minute));
    }

    [Fact]
    public void FormatShopHours_ReportsConfiguredOpenAndCloseTimes()
    {
        Assert.Equal("09:00-18:00", TimeOfDayController.FormatShopHours());
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(9 * 60, "09:00")]
    [InlineData(18 * 60, "18:00")]
    [InlineData(23 * 60 + 59, "23:59")]
    public void FormatClock_ReturnsZeroPaddedTwentyFourHourTime(int minute, string expected)
    {
        Assert.Equal(expected, TimeOfDayController.FormatClock(minute));
    }
}
