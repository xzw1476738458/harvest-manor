namespace HarvestManor.Core.Time;

public sealed class StaminaState
{
    public StaminaState(int maximum, int current)
    {
        if (maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Maximum stamina must be positive.");
        }

        if (current < 0 || current > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(current), current, "Current stamina must be between zero and maximum.");
        }

        Maximum = maximum;
        Current = current;
    }

    public int Maximum { get; }

    public int Current { get; private set; }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Current)
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
