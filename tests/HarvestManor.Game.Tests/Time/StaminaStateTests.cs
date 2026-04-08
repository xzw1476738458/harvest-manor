using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Time;

public sealed class StaminaStateTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    [InlineData(10, 11)]
    public void Constructor_ThrowsWhenValuesBreakInvariant(int maximum, int current)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StaminaState(maximum, current));
    }

    [Fact]
    public void TrySpend_ReturnsFalseWhenCostExceedsCurrentStamina()
    {
        var stamina = new StaminaState(maximum: 100, current: 10);

        var spent = stamina.TrySpend(12);

        Assert.False(spent);
        Assert.Equal(10, stamina.Current);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    public void TrySpend_ReturnsFalseWhenAmountIsNotPositive(int amount)
    {
        var stamina = new StaminaState(maximum: 100, current: 40);

        var spent = stamina.TrySpend(amount);

        Assert.False(spent);
        Assert.Equal(40, stamina.Current);
    }

    [Fact]
    public void RestoreFull_RefillsToMaximum()
    {
        var stamina = new StaminaState(maximum: 100, current: 25);

        stamina.RestoreFull();

        Assert.Equal(100, stamina.Current);
    }
}
