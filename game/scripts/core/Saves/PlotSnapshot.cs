namespace HarvestManor.Core.Saves;

public sealed record PlotSnapshot(
    int X,
    int Y,
    bool IsTilled,
    bool IsLocked,
    bool IsHarvestReady,
    string? CropId,
    int DaysGrown);
