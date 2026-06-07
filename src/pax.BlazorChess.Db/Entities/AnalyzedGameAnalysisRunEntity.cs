namespace pax.BlazorChess.Db.Entities;

public sealed class AnalyzedGameAnalysisRunEntity
{
    public Guid Id { get; set; }
    public Guid AnalyzedGameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AnalysisJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public AnalyzedGameEntity? AnalyzedGame { get; set; }
}
