using HarvestManor.Core.Inventory;

namespace HarvestManor.Core.Gathering;

public sealed class GatheringService
{
    private readonly Dictionary<string, GatheringNodeDefinition> _nodesById;
    private readonly GatheringState _state;

    public GatheringService(IEnumerable<GatheringNodeDefinition> nodes, GatheringState state)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(state);

        _nodesById = new Dictionary<string, GatheringNodeDefinition>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            ArgumentNullException.ThrowIfNull(node);
            node.Validate();

            if (_nodesById.ContainsKey(node.NodeId))
            {
                throw new ArgumentException($"Duplicate gathering node id '{node.NodeId}'.", nameof(nodes));
            }

            _nodesById.Add(node.NodeId, node);
        }

        _state = state;
    }

    public IReadOnlyDictionary<string, GatheringNodeDefinition> Nodes => _nodesById;

    public GatheringState State => _state;

    public GatheringHarvestResult TryHarvest(string nodeId, InventoryState inventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(inventory);

        if (!_nodesById.TryGetValue(nodeId, out var node))
        {
            return new GatheringHarvestResult(GatheringHarvestOutcome.UnknownNode, null);
        }

        if (_state.IsHarvested(nodeId))
        {
            return new GatheringHarvestResult(GatheringHarvestOutcome.AlreadyHarvested, node.ItemId);
        }

        if (!inventory.CanAdd(node.ItemId, 1))
        {
            return new GatheringHarvestResult(GatheringHarvestOutcome.InventoryFull, node.ItemId);
        }

        inventory.TryAdd(node.ItemId, 1);
        _state.TryMarkHarvested(nodeId);
        return new GatheringHarvestResult(GatheringHarvestOutcome.Success, node.ItemId);
    }

    public void ResetForNewDay()
    {
        _state.ResetForNewDay();
    }
}
