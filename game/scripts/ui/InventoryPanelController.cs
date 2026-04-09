using System.Linq;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class InventoryPanelController : Control
{
    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    public override void _Ready()
    {
        BodyLabel ??= GetNodeOrNull<RichTextLabel>("Panel/BodyLabel");
    }

    public void Render(InventoryState inventory, IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (BodyLabel is null)
        {
            return;
        }

        BodyLabel.Text = BuildBodyText(inventory, itemCatalog);
    }

    public static string BuildBodyText(InventoryState inventory, IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var lines = inventory.Slots
            .Select(slot => $"{ItemDisplayNameFormatter.Resolve(slot.ItemId, itemCatalog)} x{slot.Quantity}")
            .DefaultIfEmpty("Inventory empty.");

        return string.Join("\n", lines);
    }
}
