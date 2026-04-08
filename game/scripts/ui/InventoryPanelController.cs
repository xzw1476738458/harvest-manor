using System.Linq;
using Godot;
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

    public void Render(InventoryState inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (BodyLabel is null)
        {
            return;
        }

        var lines = inventory.Slots
            .Select(slot => $"{slot.ItemId} x{slot.Quantity}")
            .DefaultIfEmpty("Inventory empty.");
        BodyLabel.Text = string.Join("\n", lines);
    }
}
