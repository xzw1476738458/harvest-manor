namespace HarvestManor.Core.Progression;

public sealed class UnlockState
{
    public UnlockState(HashSet<string> unlockedPlotKeys)
    {
        UnlockedPlotKeys = unlockedPlotKeys;
    }

    public HashSet<string> UnlockedPlotKeys { get; }
}
