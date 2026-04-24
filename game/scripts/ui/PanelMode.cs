namespace HarvestManor.UI;

public enum PanelMode
{
    None,
    Shop,
    Storage
}

public readonly record struct PanelVisibility(bool InventoryVisible, bool ShopVisible, bool StorageVisible);
