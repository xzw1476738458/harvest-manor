using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;

namespace HarvestManor.Core.Saves;

public sealed record SaveGameSnapshot(
    GameDate Date,
    int MinuteOfDay,
    int Gold,
    int Stamina,
    IReadOnlyList<ItemStack> Inventory,
    IReadOnlyList<ItemStack> Storage,
    IReadOnlyList<PlotSnapshot> Plots,
    IReadOnlyList<string> UnlockedPlotKeys,
    IReadOnlyList<string> CompletedRequests);
