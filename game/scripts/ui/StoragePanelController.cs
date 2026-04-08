using System.Linq;
using Godot;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class StoragePanelController : Control
{
    private string? _storeCandidateItemId;
    private string? _withdrawCandidateItemId;

    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    [Export]
    public Button? StoreButton { get; set; }

    [Export]
    public Button? WithdrawButton { get; set; }

    [Export]
    public Button? CloseButton { get; set; }

    [Signal]
    public delegate void StoreRequestedEventHandler(string itemId);

    [Signal]
    public delegate void WithdrawRequestedEventHandler(string itemId);

    [Signal]
    public delegate void CloseRequestedEventHandler();

    public override void _Ready()
    {
        BodyLabel ??= GetNodeOrNull<RichTextLabel>("Panel/Rows/BodyLabel");
        StoreButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/StoreButton");
        WithdrawButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/WithdrawButton");
        CloseButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/CloseButton");

        if (StoreButton is not null)
        {
            StoreButton.Pressed += () =>
            {
                if (!string.IsNullOrWhiteSpace(_storeCandidateItemId))
                {
                    EmitSignal(SignalName.StoreRequested, _storeCandidateItemId);
                }
            };
        }

        if (WithdrawButton is not null)
        {
            WithdrawButton.Pressed += () =>
            {
                if (!string.IsNullOrWhiteSpace(_withdrawCandidateItemId))
                {
                    EmitSignal(SignalName.WithdrawRequested, _withdrawCandidateItemId);
                }
            };
        }

        if (CloseButton is not null)
        {
            CloseButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
        }
    }

    public void Render(InventoryState inventory, InventoryState storage)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(storage);

        if (BodyLabel is null)
        {
            return;
        }

        _storeCandidateItemId = inventory.Slots.FirstOrDefault()?.ItemId;
        _withdrawCandidateItemId = storage.Slots.FirstOrDefault()?.ItemId;

        var inventoryLines = inventory.Slots
            .Select(slot => $"{slot.ItemId} x{slot.Quantity}")
            .DefaultIfEmpty("Inventory empty.");

        var storageLines = storage.Slots
            .Select(slot => $"{slot.ItemId} x{slot.Quantity}")
            .DefaultIfEmpty("Storage empty.");

        BodyLabel.Text = "Inventory\n" +
                         string.Join("\n", inventoryLines) +
                         "\n\nStorage\n" +
                         string.Join("\n", storageLines);

        if (StoreButton is not null)
        {
            StoreButton.Text = _storeCandidateItemId is null ? "Nothing to store" : $"Store 1 {_storeCandidateItemId}";
            StoreButton.Disabled = _storeCandidateItemId is null;
        }

        if (WithdrawButton is not null)
        {
            WithdrawButton.Text = _withdrawCandidateItemId is null ? "Nothing to take" : $"Take 1 {_withdrawCandidateItemId}";
            WithdrawButton.Disabled = _withdrawCandidateItemId is null;
        }
    }
}
