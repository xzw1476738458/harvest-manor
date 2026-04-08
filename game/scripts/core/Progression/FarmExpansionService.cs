namespace HarvestManor.Core.Progression;

public sealed class FarmExpansionService
{
    public bool TryUnlockPlot(UnlockState unlocks, string plotKey, int requiredGold, int currentGold, out int updatedGold)
    {
        ArgumentNullException.ThrowIfNull(unlocks);

        if (string.IsNullOrWhiteSpace(plotKey))
        {
            throw new ArgumentException("Plot key cannot be blank.", nameof(plotKey));
        }

        if (requiredGold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredGold), requiredGold, "Required gold cannot be negative.");
        }

        if (unlocks.UnlockedPlotKeys.Contains(plotKey))
        {
            updatedGold = currentGold;
            return false;
        }

        if (currentGold < requiredGold)
        {
            updatedGold = currentGold;
            return false;
        }

        unlocks.UnlockedPlotKeys.Add(plotKey);
        updatedGold = currentGold - requiredGold;
        return true;
    }
}
