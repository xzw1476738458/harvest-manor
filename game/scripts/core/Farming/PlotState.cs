namespace HarvestManor.Core.Farming;

public sealed record PlotState(
    int X,
    int Y,
    bool IsTilled,
    bool IsLocked,
    bool IsWateredToday,
    bool IsHarvestReady,
    CropInstance? Crop)
{
    public static PlotState Wild(int x, int y) => new(x, y, false, false, false, false, null);

    public static PlotState Tilled(int x, int y) => new(x, y, true, false, false, false, null);

    public PlotState Till()
    {
        if (IsLocked)
        {
            throw new InvalidOperationException("Cannot till a locked plot.");
        }

        return this with { IsTilled = true };
    }

    public PlotState Plant(string cropId)
    {
        if (string.IsNullOrWhiteSpace(cropId))
        {
            throw new ArgumentException("Crop id cannot be blank.", nameof(cropId));
        }

        if (IsLocked)
        {
            throw new InvalidOperationException("Cannot plant on a locked plot.");
        }

        if (!IsTilled)
        {
            throw new InvalidOperationException("Cannot plant on an untilled plot.");
        }

        if (Crop is not null)
        {
            throw new InvalidOperationException("Cannot plant on a plot that already has a crop.");
        }

        return this with
        {
            Crop = new CropInstance(cropId, 0),
            IsWateredToday = false,
            IsHarvestReady = false
        };
    }

    public PlotState Water()
    {
        if (IsLocked)
        {
            throw new InvalidOperationException("Cannot water a locked plot.");
        }

        if (!IsTilled)
        {
            throw new InvalidOperationException("Cannot water an untilled plot.");
        }

        if (Crop is null)
        {
            throw new InvalidOperationException("Cannot water a plot with no planted crop.");
        }

        if (IsWateredToday)
        {
            throw new InvalidOperationException("Plot has already been watered today.");
        }

        return this with { IsWateredToday = true };
    }

    public PlotState Harvest()
    {
        if (IsLocked)
        {
            throw new InvalidOperationException("Cannot harvest a locked plot.");
        }

        if (!IsHarvestReady)
        {
            throw new InvalidOperationException("Cannot harvest a plot that is not ready.");
        }

        if (Crop is null)
        {
            throw new InvalidOperationException("Cannot harvest a plot with no crop.");
        }

        return this with { Crop = null, IsWateredToday = false, IsHarvestReady = false };
    }
}
