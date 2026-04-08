namespace HarvestManor.Core.Inventory;

public sealed record ItemStack
{
    private string _itemId = string.Empty;
    private int _quantity;

    public ItemStack(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public string ItemId
    {
        get => _itemId;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Item id cannot be empty.", nameof(value));
            }

            _itemId = value;
        }
    }

    public int Quantity
    {
        get => _quantity;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Quantity must be positive.");
            }

            _quantity = value;
        }
    }
}
