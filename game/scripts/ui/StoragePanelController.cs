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
            StoreButton.Text = BuildStoreButtonText(state);
            StoreButton.Disabled = !state.CanStore;
        }

        if (WithdrawButton is not null)
        {
            WithdrawButton.Text = BuildWithdrawButtonText(state);
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

        return new TransferUiState(
            storeCandidateItemId,
            withdrawCandidateItemId,
            canStore,
            canWithdraw,
            BuildTransferStatusText(storeCandidateItemId, withdrawCandidateItemId, canStore, canWithdraw));
    }

    public static string BuildStoreButtonText(TransferUiState state)
    {
        if (!string.IsNullOrWhiteSpace(state.StoreCandidateItemId))
        {
            return state.CanStore
                ? $"Store 1 {state.StoreCandidateItemId}"
                : $"Storage full for {state.StoreCandidateItemId}";
        }

        return "Nothing to store";
    }

    public static string BuildWithdrawButtonText(TransferUiState state)
    {
        if (!string.IsNullOrWhiteSpace(state.WithdrawCandidateItemId))
        {
            return state.CanWithdraw
                ? $"Take 1 {state.WithdrawCandidateItemId}"
                : $"Inventory full for {state.WithdrawCandidateItemId}";
        }

        return "Nothing to take";
    }

    private static string BuildTransferStatusText(
        string? storeCandidateItemId,
        string? withdrawCandidateItemId,
        bool canStore,
        bool canWithdraw)
    {
        if (canStore && canWithdraw)
        {
            return "Use Store or Take to move 1 item.";
        }

        if (canStore && !string.IsNullOrWhiteSpace(storeCandidateItemId))
        {
            return !string.IsNullOrWhiteSpace(withdrawCandidateItemId)
                ? $"Ready to store 1 {storeCandidateItemId}. Cannot take {withdrawCandidateItemId}: inventory is full."
                : $"Ready to store 1 {storeCandidateItemId}.";
        }

        if (canWithdraw && !string.IsNullOrWhiteSpace(withdrawCandidateItemId))
        {
            return !string.IsNullOrWhiteSpace(storeCandidateItemId)
                ? $"Ready to take 1 {withdrawCandidateItemId}. Cannot store {storeCandidateItemId}: storage is full."
                : $"Ready to take 1 {withdrawCandidateItemId}.";
        }

        if (!string.IsNullOrWhiteSpace(storeCandidateItemId) && !string.IsNullOrWhiteSpace(withdrawCandidateItemId))
        {
            return $"Cannot store {storeCandidateItemId}: storage is full. Cannot take {withdrawCandidateItemId}: inventory is full.";
        }

        if (!string.IsNullOrWhiteSpace(storeCandidateItemId))
        {
            return $"Cannot store {storeCandidateItemId}: storage is full.";
        }

        if (!string.IsNullOrWhiteSpace(withdrawCandidateItemId))
        {
            return $"Cannot take {withdrawCandidateItemId}: inventory is full.";
        }

        return "Nothing to move.";
    }
}
