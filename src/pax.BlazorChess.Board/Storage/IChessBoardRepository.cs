using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Board.Storage;

public interface IChessBoardRepository
{
    Task<List<EngineRunOptions>> GetEngineRunOptions(CancellationToken cancellationToken = default);
    Task StoreEngineRunOptions(List<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalyzedGameSummary>> ListAnalyzedGames(CancellationToken cancellationToken = default);
    Task<AnalysisBoard?> LoadAnalyzedGame(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveAnalyzedGame(string name, AnalysisBoard analysisBoard, Guid? id = default, CancellationToken cancellationToken = default);
    Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default);
}

public sealed record AnalyzedGameSummary(Guid Id, string Name, DateTimeOffset UpdatedAt);
