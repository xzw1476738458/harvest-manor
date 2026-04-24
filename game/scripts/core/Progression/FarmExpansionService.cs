using HarvestManor.Core.Economy;

namespace HarvestManor.Core.Progression;

public sealed class FarmExpansionService
{
    public bool TryUnlockPlot(UnlockState unlocks, string plotKey, int requiredGold, Wallet wallet)
    {
        ArgumentNullException.ThrowIfNull(unlocks);
        ArgumentNullException.ThrowIfNull(wallet);

        if (unlocks.Contains(plotKey))
        {
            return false;
        }

        if (!wallet.TrySpend(requiredGold))
        {
            return false;
        }

        return unlocks.TryUnlock(plotKey);
    }
}
