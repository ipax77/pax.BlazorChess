using System.Text.Json;

namespace pax.BlazorChess.Board.Storage;

public static class GameAnalysisRunSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        MaxDepth = 128
    };

    public static string Serialize(GameAnalysisRunSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static GameAnalysisRunSnapshot Restore(string json)
        => JsonSerializer.Deserialize<GameAnalysisRunSnapshot>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize game analysis run snapshot.");
}
