namespace HarvestManor.Core.Economy;

public sealed class Wallet
{
    public Wallet(int gold)
    {
        if (gold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gold), gold, "Starting gold cannot be negative.");
        }

        Gold = gold;
    }

    public int Gold { get; private set; }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Gold)
        {
            return false;
        }

        Gold -= amount;
        return true;
    }

    public void Earn(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Gold += amount;
    }
}
