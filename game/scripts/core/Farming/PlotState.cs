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

    public PlotState Till() => this with { IsTilled = true };

    public PlotState Plant(string cropId)
    {
        return this with
        {
            Crop = new CropInstance(cropId, 0),
            IsHarvestReady = false
        };
    }

    public PlotState Water() => this with { IsWateredToday = true };
}
