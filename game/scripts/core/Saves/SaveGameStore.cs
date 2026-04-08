using System.Text.Json;

namespace HarvestManor.Core.Saves;

public static class SaveGameStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

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

        SaveGameSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<SaveGameSnapshot>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Save payload format was invalid.", exception);
        }

        if (snapshot is null)
        {
            throw new InvalidDataException("Save payload was empty.");
        }

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

        if (snapshot.Inventory is null ||
            snapshot.Storage is null ||
            snapshot.Plots is null ||
            snapshot.UnlockedPlotKeys is null ||
            snapshot.CompletedRequests is null)
        {
            throw new InvalidDataException("Save payload is missing required collections.");
        }

        if (snapshot.Inventory.Any(stack => stack is null) ||
            snapshot.Storage.Any(stack => stack is null) ||
            snapshot.Plots.Any(plot => plot is null) ||
            snapshot.UnlockedPlotKeys.Any(key => key is null) ||
            snapshot.CompletedRequests.Any(id => id is null))
        {
            throw new InvalidDataException("Save payload contains null entries.");
        }
    }
}
