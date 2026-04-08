using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Time;

public sealed class StaminaStateTests
{
    [Fact]
    public void TrySpend_ReturnsFalseWhenCostExceedsCurrentStamina()
    {
        var stamina = new StaminaState(maximum: 100, current: 10);

        var spent = stamina.TrySpend(12);

        Assert.False(spent);
        Assert.Equal(10, stamina.Current);
    }

    [Fact]
    public void RestoreFull_RefillsToMaximum()
    {
        var stamina = new StaminaState(maximum: 100, current: 25);

        stamina.RestoreFull();

        Assert.Equal(100, stamina.Current);
    }
}
