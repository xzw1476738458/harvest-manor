using System.Linq;
using Godot;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class StoragePanelController : Control
{
    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    public override void _Ready()
    {
        BodyLabel ??= GetNodeOrNull<RichTextLabel>("Panel/BodyLabel");
    }

    public void Render(InventoryState storage)
    {
        ArgumentNullException.ThrowIfNull(storage);

        if (BodyLabel is null)
        {
            return;
        }

        var lines = storage.Slots
            .Select(slot => $"{slot.ItemId} x{slot.Quantity}")
            .DefaultIfEmpty("Storage empty.");
        BodyLabel.Text = string.Join("\n", lines);
    }
}
