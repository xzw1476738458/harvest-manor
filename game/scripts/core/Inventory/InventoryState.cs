namespace HarvestManor.Core.Inventory;

public sealed class InventoryState
{
    private readonly int _slotCapacity;
    private readonly int _maxStackSize;
    private readonly List<ItemStack> _slots = new();
    private readonly IReadOnlyList<ItemStack> _slotsView;

    public InventoryState(int slotCapacity, int maxStackSize)
    {
        if (slotCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotCapacity), slotCapacity, "Slot capacity must be positive.");
        }

        if (maxStackSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStackSize), maxStackSize, "Max stack size must be positive.");
        }

        _slotCapacity = slotCapacity;
        _maxStackSize = maxStackSize;
        _slotsView = _slots.AsReadOnly();
    }

    public IReadOnlyList<ItemStack> Slots => _slotsView;

    public bool CanAdd(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        var remaining = quantity;

        foreach (var stack in _slots)
        {
            if (stack.ItemId != itemId || stack.Quantity >= _maxStackSize)
            {
                continue;
            }

            remaining -= _maxStackSize - stack.Quantity;
            if (remaining <= 0)
            {
                return true;
            }
        }

        var freeSlots = _slotCapacity - _slots.Count;
        return freeSlots > 0 && remaining <= freeSlots * _maxStackSize;
    }

    public bool TryAdd(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        for (var index = 0; index < _slots.Count && quantity > 0; index++)
        {
            var stack = _slots[index];
            if (stack.ItemId != itemId || stack.Quantity >= _maxStackSize)
            {
                continue;
            }

            var newQuantity = Math.Min(_maxStackSize, stack.Quantity + quantity);
            var consumed = newQuantity - stack.Quantity;
            quantity -= consumed;
            _slots[index] = stack with { Quantity = newQuantity };
        }

        while (quantity > 0 && _slots.Count < _slotCapacity)
        {
            var nextQuantity = Math.Min(_maxStackSize, quantity);
            _slots.Add(new ItemStack(itemId, nextQuantity));
            quantity -= nextQuantity;
        }

        return quantity == 0;
    }

    public bool TryRemove(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0 || GetQuantity(itemId) < quantity)
        {
            return false;
        }

        for (var index = _slots.Count - 1; index >= 0 && quantity > 0; index--)
        {
            var stack = _slots[index];
            if (stack.ItemId != itemId)
            {
                continue;
            }

            var removeAmount = Math.Min(stack.Quantity, quantity);
            quantity -= removeAmount;
            var remaining = stack.Quantity - removeAmount;

            if (remaining == 0)
            {
                _slots.RemoveAt(index);
            }
            else
            {
                _slots[index] = stack with { Quantity = remaining };
            }
        }

        return true;
    }

    public int GetQuantity(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return _slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Quantity);
    }

    internal List<ItemStack> CreateSnapshot()
    {
        return CloneStacks(_slots);
    }

    internal void RestoreSnapshot(IReadOnlyList<ItemStack> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _slots.Clear();
        _slots.AddRange(CloneStacks(snapshot));
    }

    private static List<ItemStack> CloneStacks(IEnumerable<ItemStack> source)
    {
        var clone = new List<ItemStack>();
        foreach (var stack in source)
        {
            ArgumentNullException.ThrowIfNull(stack);
            clone.Add(stack with { });
        }

        return clone;
    }
}
