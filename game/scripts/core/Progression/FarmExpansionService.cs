namespace HarvestManor.Core.Progression;

public sealed class FarmExpansionService
{
    public bool TryUnlockPlot(UnlockState unlocks, string plotKey, int requiredGold, int currentGold, out int updatedGold)
    {
        if (currentGold < requiredGold || unlocks.UnlockedPlotKeys.Contains(plotKey))
        {
            updatedGold = currentGold;
            return false;
        }

        unlocks.UnlockedPlotKeys.Add(plotKey);
        updatedGold = currentGold - requiredGold;
        return true;
    }
}
