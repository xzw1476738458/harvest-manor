using System.Text.Json;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;

namespace HarvestManor.Core.Saves;

public static class SaveGameStore
{
    private sealed class SaveGameSnapshotPayload
    {
        public GameDate? Date { get; init; }

        public int MinuteOfDay { get; init; }

        public int Gold { get; init; }

        public int Stamina { get; init; }

        public List<ItemStack>? Inventory { get; init; }

        public List<ItemStack>? Storage { get; init; }

        public List<PlotSnapshot>? Plots { get; init; }

        public List<string>? UnlockedPlotKeys { get; init; }

        public List<string>? CompletedRequests { get; init; }

        public List<string>? HarvestedGatheringNodeIds { get; init; }
    }

    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.WriteOptions;

    public static string Serialize(SaveGameSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static SaveGameSnapshot Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Save payload was empty.");
        }

        SaveGameSnapshotPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SaveGameSnapshotPayload>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Save payload format was invalid.", exception);
        }

        if (payload is null)
        {
            throw new InvalidDataException("Save payload was empty.");
        }

        var snapshot = new SaveGameSnapshot(
            payload.Date ?? throw new InvalidDataException("Save payload is missing a valid date."),
            payload.MinuteOfDay,
            payload.Gold,
            payload.Stamina,
            payload.Inventory ?? throw new InvalidDataException("Save payload is missing inventory data."),
            payload.Storage ?? throw new InvalidDataException("Save payload is missing storage data."),
            payload.Plots ?? throw new InvalidDataException("Save payload is missing plot data."),
            payload.UnlockedPlotKeys ?? new List<string>(),
            payload.CompletedRequests ?? new List<string>(),
            payload.HarvestedGatheringNodeIds ?? new List<string>());

        ValidateSnapshot(snapshot);
        return snapshot;
    }

    private static void ValidateSnapshot(SaveGameSnapshot snapshot)
    {
        if (snapshot.Date.Day <= 0)
        {
            throw new InvalidDataException("Save payload contains an invalid day.");
        }

        if (snapshot.MinuteOfDay < 0)
        {
            throw new InvalidDataException("Save payload contains an invalid minute-of-day.");
        }

        if (snapshot.Gold < 0)
        {
            throw new InvalidDataException("Save payload contains invalid gold.");
        }

        if (snapshot.Stamina < 0)
        {
            throw new InvalidDataException("Save payload contains invalid stamina.");
        }

        if (snapshot.Inventory.Any(stack => stack is null) ||
            snapshot.Storage.Any(stack => stack is null) ||
            snapshot.Plots.Any(plot => plot is null) ||
            snapshot.UnlockedPlotKeys.Any(key => key is null) ||
            snapshot.CompletedRequests.Any(id => id is null) ||
            snapshot.HarvestedGatheringNodeIds.Any(id => id is null))
        {
            throw new InvalidDataException("Save payload contains null entries.");
        }
    }
}
