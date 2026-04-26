namespace HarvestManor.Core.Progression;

public sealed class ExpansionTierService
{
    public sealed record Tier(int RingIndex, int UnlockCost, IReadOnlyList<string> PlotKeys);

    public sealed record TierConfiguration(int InclusiveMaxDistance, int UnlockCost);

    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly Dictionary<string, int> _costsByPlotKey;
    private readonly IReadOnlyList<Tier> _lockedTiers;
    private readonly IReadOnlySet<string> _defaultUnlockedPlotKeys;

    public ExpansionTierService(int gridWidth, int gridHeight, IReadOnlyList<TierConfiguration> tiers)
    {
        if (gridWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gridWidth), gridWidth, "Grid width must be positive.");
        }

        if (gridHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gridHeight), gridHeight, "Grid height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(tiers);
        if (tiers.Count == 0)
        {
            throw new ArgumentException("Tiers must contain at least the free starter tier.", nameof(tiers));
        }

        if (tiers[0].UnlockCost != 0)
        {
            throw new ArgumentException("The first tier must be free (cost 0).", nameof(tiers));
        }

        for (var i = 1; i < tiers.Count; i++)
        {
            if (tiers[i].InclusiveMaxDistance <= tiers[i - 1].InclusiveMaxDistance)
            {
                throw new ArgumentException("Tier max distances must be strictly increasing.", nameof(tiers));
            }
        }

        _gridWidth = gridWidth;
        _gridHeight = gridHeight;

        var costsByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        var lockedTiers = new List<Tier>();
        var defaultUnlockedPlotKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
        {
            var lowerExclusive = tierIndex == 0 ? -1 : tiers[tierIndex - 1].InclusiveMaxDistance;
            var upperInclusive = tiers[tierIndex].InclusiveMaxDistance;

            var keysForTier = EnumeratePlotKeysWithinDistanceBand(gridWidth, gridHeight, lowerExclusive, upperInclusive).ToList();

            if (tierIndex == 0)
            {
                foreach (var key in keysForTier)
                {
                    defaultUnlockedPlotKeys.Add(key);
                }
                continue;
            }

            if (keysForTier.Count == 0)
            {
                continue;
            }

            var cost = tiers[tierIndex].UnlockCost;
            foreach (var key in keysForTier)
            {
                costsByKey[key] = cost;
            }

            lockedTiers.Add(new Tier(tierIndex, cost, keysForTier));
        }

        _costsByPlotKey = costsByKey;
        _lockedTiers = lockedTiers;
        _defaultUnlockedPlotKeys = defaultUnlockedPlotKeys;
    }

    public IReadOnlySet<string> DefaultUnlockedPlotKeys => _defaultUnlockedPlotKeys;

    public IReadOnlyList<Tier> EnumerateLockedTiers() => _lockedTiers;

    public Tier? GetActiveTier(UnlockState unlockState)
    {
        ArgumentNullException.ThrowIfNull(unlockState);

        foreach (var tier in _lockedTiers)
        {
            foreach (var key in tier.PlotKeys)
            {
                if (!unlockState.Contains(key))
                {
                    return tier;
                }
            }
        }

        return null;
    }

    public int? GetUnlockCost(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _gridWidth || y >= _gridHeight)
        {
            return null;
        }

        var key = $"{x},{y}";
        return _costsByPlotKey.TryGetValue(key, out var cost) ? cost : null;
    }

    public int? GetUnlockCost(string plotKey)
    {
        if (string.IsNullOrWhiteSpace(plotKey))
        {
            return null;
        }

        return _costsByPlotKey.TryGetValue(plotKey, out var cost) ? cost : null;
    }

    public static ExpansionTierService CreateDefault()
    {
        return new ExpansionTierService(
            gridWidth: 6,
            gridHeight: 6,
            tiers: new[]
            {
                new TierConfiguration(InclusiveMaxDistance: 1, UnlockCost: 0),
                new TierConfiguration(InclusiveMaxDistance: 2, UnlockCost: 120),
                new TierConfiguration(InclusiveMaxDistance: 3, UnlockCost: 280),
                new TierConfiguration(InclusiveMaxDistance: 4, UnlockCost: 600),
                new TierConfiguration(InclusiveMaxDistance: 5, UnlockCost: 1200),
            });
    }

    private static IEnumerable<string> EnumeratePlotKeysWithinDistanceBand(
        int width,
        int height,
        int lowerExclusive,
        int upperInclusive)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var distance = Math.Max(x, y);
                if (distance > lowerExclusive && distance <= upperInclusive)
                {
                    yield return $"{x},{y}";
                }
            }
        }
    }
}
