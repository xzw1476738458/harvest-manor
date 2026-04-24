namespace HarvestManor.Core.Economy;

public sealed record ShopOffer(string ItemId, int BuyPrice, int SellPrice)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
        {
            throw new InvalidDataException("Shop offer has a blank item id.");
        }

        if (BuyPrice < 0)
        {
            throw new InvalidDataException($"Shop offer '{ItemId}' has a negative buy price ({BuyPrice}).");
        }

        if (SellPrice < 0)
        {
            throw new InvalidDataException($"Shop offer '{ItemId}' has a negative sell price ({SellPrice}).");
        }
    }
}
