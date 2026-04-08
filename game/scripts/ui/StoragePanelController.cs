using System.Linq;
using Godot;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class StoragePanelController : Control
{
    public readonly record struct TransferUiState(
        string? StoreCandidateItemId,
        string? WithdrawCandidateItemId,
        bool CanStore,
        bool CanWithdraw,
        string StatusText);

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

        var state = EvaluateTransferState(inventory, storage);
        _storeCandidateItemId = state.StoreCandidateItemId;
        _withdrawCandidateItemId = state.WithdrawCandidateItemId;

        var inventoryLines = inventory.Slots
            .Select(slot => $"{slot.ItemId} x{slot.Quantity}")
            .DefaultIfEmpty("Inventory empty.");

        var storageLines = storage.Slots
            .Select(slot => $"{slot.ItemId} x{slot.Quantity}")
            .DefaultIfEmpty("Storage empty.");

        BodyLabel.Text = "Inventory\n" +
                         string.Join("\n", inventoryLines) +
                         "\n\nStorage\n" +
                         string.Join("\n", storageLines) +
                         $"\n\nStatus\n{state.StatusText}";

        if (StoreButton is not null)
        {
            StoreButton.Text = _storeCandidateItemId is null ? "Nothing to store" : $"Store 1 {_storeCandidateItemId}";
            StoreButton.Disabled = !state.CanStore;
        }

        if (WithdrawButton is not null)
        {
            WithdrawButton.Text = _withdrawCandidateItemId is null ? "Nothing to take" : $"Take 1 {_withdrawCandidateItemId}";
            WithdrawButton.Disabled = !state.CanWithdraw;
        }
    }

    public static TransferUiState EvaluateTransferState(InventoryState inventory, InventoryState storage)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(storage);

        var storeCandidateItemId = inventory.Slots
            .Select(static slot => slot.ItemId)
            .FirstOrDefault(itemId => storage.CanAdd(itemId, 1))
            ?? inventory.Slots.FirstOrDefault()?.ItemId;

        var withdrawCandidateItemId = storage.Slots
            .Select(static slot => slot.ItemId)
            .FirstOrDefault(itemId => inventory.CanAdd(itemId, 1))
            ?? storage.Slots.FirstOrDefault()?.ItemId;

        var canStore = storeCandidateItemId is not null && storage.CanAdd(storeCandidateItemId, 1);
        var canWithdraw = withdrawCandidateItemId is not null && inventory.CanAdd(withdrawCandidateItemId, 1);

        string statusText;
        if (canStore && canWithdraw)
        {
            statusText = "Use Store or Take to move 1 item.";
        }
        else if (canStore)
        {
            statusText = withdrawCandidateItemId is not null
                ? "Can store 1. Inventory is full for the selected item."
                : "Ready to store 1.";
        }
        else if (canWithdraw)
        {
            statusText = storeCandidateItemId is not null
                ? "Can take 1. Storage is full for the selected item."
                : "Ready to take 1.";
        }
        else if (storeCandidateItemId is not null && !canStore)
        {
            statusText = "Storage is full for the selected item.";
        }
        else if (withdrawCandidateItemId is not null && !canWithdraw)
        {
            statusText = "Inventory is full for the selected item.";
        }
        else
        {
            statusText = "Nothing to move.";
        }

        return new TransferUiState(storeCandidateItemId, withdrawCandidateItemId, canStore, canWithdraw, statusText);
    }
}
