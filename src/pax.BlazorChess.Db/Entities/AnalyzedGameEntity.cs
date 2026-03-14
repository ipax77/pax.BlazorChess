namespace pax.BlazorChess.Db.Entities;

public sealed class AnalyzedGameEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InitialFen { get; set; } = string.Empty;
    public string? Pgn { get; set; }
    public string AnalysisJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
