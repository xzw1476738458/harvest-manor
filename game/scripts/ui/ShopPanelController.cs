using Godot;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class ShopPanelController : Control
{
    public readonly record struct OfferUiState(int InventoryCount, int Gold, bool CanBuy, bool CanSell, string StatusText);

    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    [Export]
    public Button? PreviousOfferButton { get; set; }

    [Export]
    public Button? NextOfferButton { get; set; }

    [Export]
    public Button? BuyButton { get; set; }

    [Export]
    public Button? SellButton { get; set; }

    [Export]
    public Button? CloseButton { get; set; }

    [Signal]
    public delegate void PreviousOfferRequestedEventHandler();

    [Signal]
    public delegate void NextOfferRequestedEventHandler();

    [Signal]
    public delegate void BuyRequestedEventHandler();

    [Signal]
    public delegate void SellRequestedEventHandler();

    [Signal]
    public delegate void CloseRequestedEventHandler();

    public override void _Ready()
    {
        BodyLabel ??= GetNodeOrNull<RichTextLabel>("Panel/Rows/BodyLabel");
        PreviousOfferButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/PreviousOfferButton");
        NextOfferButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/NextOfferButton");
        BuyButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/BuyButton");
        SellButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/SellButton");
        CloseButton ??= GetNodeOrNull<Button>("Panel/Rows/ButtonRow/CloseButton");

        if (PreviousOfferButton is not null)
        {
            PreviousOfferButton.Pressed += () => EmitSignal(SignalName.PreviousOfferRequested);
        }

        if (NextOfferButton is not null)
        {
            NextOfferButton.Pressed += () => EmitSignal(SignalName.NextOfferRequested);
        }

        if (BuyButton is not null)
        {
            BuyButton.Pressed += () => EmitSignal(SignalName.BuyRequested);
        }

        if (SellButton is not null)
        {
            SellButton.Pressed += () => EmitSignal(SignalName.SellRequested);
        }

        if (CloseButton is not null)
        {
            CloseButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
        }
    }

    public void Render(IReadOnlyList<ShopOffer> offers, int selectedOfferIndex, InventoryState? inventory, Wallet? wallet)
    {
        ArgumentNullException.ThrowIfNull(offers);

        if (BodyLabel is null)
        {
            return;
        }

        if (offers.Count == 0)
        {
            BodyLabel.Text = "No offers available.";
            SetButtonState(hasOffer: false, canBuy: false, canSell: false);
            return;
        }

        var clampedIndex = Math.Clamp(selectedOfferIndex, 0, offers.Count - 1);
        var offer = offers[clampedIndex];
        var state = EvaluateOfferState(offer, inventory, wallet);

        BodyLabel.Text = $"Offer {clampedIndex + 1}/{offers.Count}\n" +
                         $"Item: {offer.ItemId}\n" +
                         $"Gold: {state.Gold}\n" +
                         $"Owned: {state.InventoryCount}\n" +
                         $"Buy: {offer.BuyPrice}g\n" +
                         $"Sell: {offer.SellPrice}g\n" +
                         $"Status: {state.StatusText}";

        if (BuyButton is not null)
        {
            BuyButton.Text = offer.BuyPrice > 0 ? $"Buy 1 ({offer.BuyPrice}g)" : "Buy unavailable";
        }

        if (SellButton is not null)
        {
            SellButton.Text = offer.SellPrice > 0 ? $"Sell 1 ({offer.SellPrice}g)" : "Sell unavailable";
        }

        SetButtonState(hasOffer: true, state.CanBuy, state.CanSell);
    }

    public static OfferUiState EvaluateOfferState(ShopOffer offer, InventoryState? inventory, Wallet? wallet)
    {
        var inventoryCount = inventory?.GetQuantity(offer.ItemId) ?? 0;
        var gold = wallet?.Gold ?? 0;
        var hasInventorySpace = inventory?.CanAdd(offer.ItemId, 1) ?? false;
        var canAfford = offer.BuyPrice > 0 && wallet is not null && gold >= offer.BuyPrice;
        var canBuy = canAfford && hasInventorySpace;
        var canSell = offer.SellPrice > 0 && inventoryCount > 0;

        string statusText;
        if (canBuy && canSell)
        {
            statusText = "Ready to buy or sell 1.";
        }
        else if (canSell && offer.BuyPrice > 0 && inventory is not null && !hasInventorySpace)
        {
            statusText = "Ready to sell 1. Cannot buy 1: inventory full.";
        }
        else if (canSell && offer.BuyPrice > 0 && wallet is not null && !canAfford)
        {
            statusText = $"Ready to sell 1. Need {offer.BuyPrice - gold}g more to buy 1.";
        }
        else if (offer.BuyPrice > 0 && inventory is not null && !hasInventorySpace)
        {
            statusText = "Inventory full for selected offer.";
        }
        else if (offer.BuyPrice > 0 && wallet is not null && !canAfford)
        {
            statusText = $"Need {offer.BuyPrice - gold}g more to buy 1.";
        }
        else if (canBuy)
        {
            statusText = "Ready to buy 1.";
        }
        else if (canSell)
        {
            statusText = "Ready to sell 1.";
        }
        else
        {
            statusText = "Browse offers with < and >.";
        }

        return new OfferUiState(inventoryCount, gold, canBuy, canSell, statusText);
    }

    private void SetButtonState(bool hasOffer, bool canBuy, bool canSell)
    {
        if (PreviousOfferButton is not null)
        {
            PreviousOfferButton.Disabled = !hasOffer;
        }

        if (NextOfferButton is not null)
        {
            NextOfferButton.Disabled = !hasOffer;
        }

        if (BuyButton is not null)
        {
            BuyButton.Disabled = !canBuy;
        }

        if (SellButton is not null)
        {
            SellButton.Disabled = !canSell;
        }
    }
}
