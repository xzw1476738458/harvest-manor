using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;

namespace HarvestManor.Core.Saves;

public sealed record SaveGameSnapshot(
    GameDate Date,
    int MinuteOfDay,
    int Gold,
    int Stamina,
    List<ItemStack> Inventory,
    List<ItemStack> Storage,
    List<PlotSnapshot> Plots,
    List<string> UnlockedPlotKeys,
    List<string> CompletedRequests);
