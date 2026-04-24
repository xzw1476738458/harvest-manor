namespace HarvestManor.Core.Progression;

public sealed class UnlockState
{
    private readonly HashSet<string> _unlockedPlotKeys;

    public UnlockState(HashSet<string> unlockedPlotKeys)
    {
        ArgumentNullException.ThrowIfNull(unlockedPlotKeys);
        _unlockedPlotKeys = unlockedPlotKeys;
    }

    public IReadOnlySet<string> UnlockedPlotKeys => _unlockedPlotKeys;

    public bool Contains(string plotKey) => _unlockedPlotKeys.Contains(plotKey);

    public bool TryUnlock(string plotKey)
    {
        if (string.IsNullOrWhiteSpace(plotKey))
        {
            return false;
        }

        return _unlockedPlotKeys.Add(plotKey);
    }

    public void Reset(IEnumerable<string> plotKeys)
    {
        ArgumentNullException.ThrowIfNull(plotKeys);
        _unlockedPlotKeys.Clear();
        foreach (var key in plotKeys.Distinct(StringComparer.Ordinal))
        {
            _unlockedPlotKeys.Add(key);
        }
    }
}
