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
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static SaveGameSnapshot Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SaveGameSnapshot>(json, JsonOptions)
            ?? throw new InvalidDataException("Save payload was empty.");
    }
}
