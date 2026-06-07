namespace pax.BlazorChess.Db.Entities;

public sealed class AnalyzedGameEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InitialFen { get; set; } = string.Empty;
    public string? Pgn { get; set; }
    public string AnalysisJson { get; set; } = string.Empty;
    public string? Event { get; set; }
    public string? Site { get; set; }
    public string? Date { get; set; }
    public string? Round { get; set; }
    public string? White { get; set; }
    public string? Black { get; set; }
    public string? Result { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
