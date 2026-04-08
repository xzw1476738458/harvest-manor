using System.Linq;
using Godot;
using HarvestManor.Core.Economy;

namespace HarvestManor.UI;

public partial class ShopPanelController : Control
{
    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    public override void _Ready()
    {
        BodyLabel ??= GetNodeOrNull<RichTextLabel>("Panel/BodyLabel");
    }

    public void Render(IEnumerable<ShopOffer> offers)
    {
        ArgumentNullException.ThrowIfNull(offers);

        if (BodyLabel is null)
        {
            return;
        }

        var lines = offers
            .Select(offer => $"{offer.ItemId} buy:{offer.BuyPrice} sell:{offer.SellPrice}")
            .DefaultIfEmpty("No offers available.");
        BodyLabel.Text = string.Join("\n", lines);
    }
}
