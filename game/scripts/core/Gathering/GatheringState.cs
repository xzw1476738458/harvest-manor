namespace HarvestManor.Core.Gathering;

public sealed class GatheringState
{
    private readonly HashSet<string> _harvested;

    public GatheringState()
        : this(Array.Empty<string>())
    {
    }

    public GatheringState(IEnumerable<string> harvestedNodeIds)
    {
        ArgumentNullException.ThrowIfNull(harvestedNodeIds);
        _harvested = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in harvestedNodeIds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            _harvested.Add(id);
        }
    }

    public IReadOnlySet<string> HarvestedNodeIds => _harvested;

    public bool IsHarvested(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return _harvested.Contains(nodeId);
    }

    public bool TryMarkHarvested(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return _harvested.Add(nodeId);
    }

    public void ResetForNewDay()
    {
        _harvested.Clear();
    }

    public void Reset(IEnumerable<string> harvestedNodeIds)
    {
        ArgumentNullException.ThrowIfNull(harvestedNodeIds);
        _harvested.Clear();
        foreach (var id in harvestedNodeIds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            _harvested.Add(id);
        }
    }
}
