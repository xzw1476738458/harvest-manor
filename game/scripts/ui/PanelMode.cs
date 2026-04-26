namespace HarvestManor.UI;

public enum PanelMode
{
    None,
    Shop,
    Storage,
    Inventory
}

public readonly record struct PanelVisibility(bool InventoryVisible, bool ShopVisible, bool StorageVisible);
