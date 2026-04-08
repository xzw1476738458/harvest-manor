namespace HarvestManor.Core.Time;

public sealed class StaminaState
{
    public StaminaState(int maximum, int current)
    {
        Maximum = maximum;
        Current = current;
    }

    public int Maximum { get; }

    public int Current { get; private set; }

    public bool TrySpend(int amount)
    {
        if (amount > Current)
        {
            return false;
        }

        Current -= amount;
        return true;
    }

    public void RestoreFull()
    {
        Current = Maximum;
    }
}
