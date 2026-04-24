using System.Linq;
using Godot;
using HarvestManor.Core.Content;
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

    public void Render(
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
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
        BodyLabel.Text = BuildBodyText(inventory, storage, itemCatalog, state);

        if (StoreButton is not null)
        {
            StoreButton.Text = BuildStoreButtonText(state, itemCatalog);
            StoreButton.Disabled = !state.CanStore;
        }

        if (WithdrawButton is not null)
        {
            WithdrawButton.Text = BuildWithdrawButtonText(state, itemCatalog);
            WithdrawButton.Disabled = !state.CanWithdraw;
        }
    }

    public static string BuildBodyText(
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(storage);

        return BuildBodyText(inventory, storage, itemCatalog, EvaluateTransferState(inventory, storage));
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
        => BuildStoreButtonText(state, itemCatalog: null);

    public static string BuildStoreButtonText(
        TransferUiState state,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        if (!string.IsNullOrWhiteSpace(state.StoreCandidateItemId))
        {
            var displayName = ItemDisplayNameFormatter.Resolve(state.StoreCandidateItemId, itemCatalog);
            return state.CanStore
                ? $"Store 1 {displayName}"
                : $"Storage full for {displayName}";
        }

        return "Nothing to store";
    }

    public static string BuildWithdrawButtonText(TransferUiState state)
        => BuildWithdrawButtonText(state, itemCatalog: null);

    public static string BuildWithdrawButtonText(
        TransferUiState state,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        if (!string.IsNullOrWhiteSpace(state.WithdrawCandidateItemId))
        {
            var displayName = ItemDisplayNameFormatter.Resolve(state.WithdrawCandidateItemId, itemCatalog);
            return state.CanWithdraw
                ? $"Take 1 {displayName}"
                : $"Inventory full for {displayName}";
        }

        return "Nothing to take";
    }

    private static string BuildTransferStatusText(
        string? storeCandidateItemId,
        string? withdrawCandidateItemId,
        bool canStore,
        bool canWithdraw)
        => BuildTransferStatusText(storeCandidateItemId, withdrawCandidateItemId, canStore, canWithdraw, itemCatalog: null);

    private static string BuildBodyText(
        InventoryState inventory,
        InventoryState storage,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog,
        TransferUiState state)
    {
        var inventoryLines = inventory.Slots
            .Select(slot => $"{ItemDisplayNameFormatter.Resolve(slot.ItemId, itemCatalog)} x{slot.Quantity}")
            .DefaultIfEmpty("Inventory empty.");

        var storageLines = storage.Slots
            .Select(slot => $"{ItemDisplayNameFormatter.Resolve(slot.ItemId, itemCatalog)} x{slot.Quantity}")
            .DefaultIfEmpty("Storage empty.");

        return "Inventory\n" +
               string.Join("\n", inventoryLines) +
               "\n\nStorage\n" +
               string.Join("\n", storageLines) +
               $"\n\nStatus\n{BuildTransferStatusText(state.StoreCandidateItemId, state.WithdrawCandidateItemId, state.CanStore, state.CanWithdraw, itemCatalog)}";
    }

    private static string BuildTransferStatusText(
        string? storeCandidateItemId,
        string? withdrawCandidateItemId,
        bool canStore,
        bool canWithdraw,
        IReadOnlyDictionary<string, ItemDefinition>? itemCatalog)
    {
        var storeDisplayName = string.IsNullOrWhiteSpace(storeCandidateItemId)
            ? null
            : ItemDisplayNameFormatter.Resolve(storeCandidateItemId, itemCatalog);
        var withdrawDisplayName = string.IsNullOrWhiteSpace(withdrawCandidateItemId)
            ? null
            : ItemDisplayNameFormatter.Resolve(withdrawCandidateItemId, itemCatalog);

        if (canStore && canWithdraw)
        {
            return "Use Store or Take to move 1 item.";
        }

        if (canStore && !string.IsNullOrWhiteSpace(storeDisplayName))
        {
            return !string.IsNullOrWhiteSpace(withdrawDisplayName)
                ? $"Ready to store 1 {storeDisplayName}. Cannot take {withdrawDisplayName}: inventory is full."
                : $"Ready to store 1 {storeDisplayName}.";
        }

        if (canWithdraw && !string.IsNullOrWhiteSpace(withdrawDisplayName))
        {
            return !string.IsNullOrWhiteSpace(storeDisplayName)
                ? $"Ready to take 1 {withdrawDisplayName}. Cannot store {storeDisplayName}: storage is full."
                : $"Ready to take 1 {withdrawDisplayName}.";
        }

        if (!string.IsNullOrWhiteSpace(storeDisplayName) && !string.IsNullOrWhiteSpace(withdrawDisplayName))
        {
            return $"Cannot store {storeDisplayName}: storage is full. Cannot take {withdrawDisplayName}: inventory is full.";
        }

        if (!string.IsNullOrWhiteSpace(storeDisplayName))
        {
            return $"Cannot store {storeDisplayName}: storage is full.";
        }

        if (!string.IsNullOrWhiteSpace(withdrawDisplayName))
        {
            return $"Cannot take {withdrawDisplayName}: inventory is full.";
        }

        return "Nothing to move.";
    }
}
