using pax.BlazorChess.Board;
using pax.chess;
using pax.chess.Analyze;
using pax.chess.Extensions;
using pax.uciChessEngine.EngineServices;
using System.Security.Cryptography;
using System.Text;

namespace pax.BlazorChess.Board.Storage;

public interface IChessBoardRepository
{
    Task<List<EngineRunOptions>> GetEngineRunOptions(CancellationToken cancellationToken = default);
    Task StoreEngineRunOptions(IReadOnlyList<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalyzedGameSummary>> ListAnalyzedGames(CancellationToken cancellationToken = default);
    Task<AnalysisBoard?> LoadAnalyzedGame(Guid id, CancellationToken cancellationToken = default);
    Task<AnalyzedGameDetails?> LoadAnalyzedGameDetails(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveAnalyzedGame(
        string name,
        AnalysisBoard analysisBoard,
        Guid? id = default,
        AnalyzedGameMetadata? metadata = default,
        CancellationToken cancellationToken = default);
    Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalyzedGameAnalysisRunSummary>> ListAnalyzedGameAnalysisRuns(
        Guid analyzedGameId,
        CancellationToken cancellationToken = default);
    Task<AnalyzedGameAnalysisRunDetails?> LoadAnalyzedGameAnalysisRun(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<Guid> SaveAnalyzedGameAnalysisRun(
        Guid analyzedGameId,
        string name,
        GameAnalysisRunSnapshot snapshot,
        Guid? id = default,
        CancellationToken cancellationToken = default);
    Task DeleteAnalyzedGameAnalysisRun(Guid id, CancellationToken cancellationToken = default);
}

public sealed record AnalyzedGameSummary(Guid Id, string Name, DateTimeOffset UpdatedAt);

public sealed record AnalyzedGameDetails(
    Guid Id,
    string Name,
    AnalysisBoard AnalysisBoard,
    AnalyzedGameMetadata Metadata,
    DateTimeOffset UpdatedAt);

public sealed record AnalyzedGameMetadata
{
    public string? Event { get; init; }
    public string? Site { get; init; }
    public string? Date { get; init; }
    public string? Round { get; init; }
    public string? White { get; init; }
    public string? Black { get; init; }
    public string? Result { get; init; }

    public static AnalyzedGameMetadata Empty { get; } = new();
}

public sealed record AnalyzedGameAnalysisRunSummary(
    Guid Id,
    Guid AnalyzedGameId,
    string Name,
    DateTimeOffset UpdatedAt);

public sealed record AnalyzedGameAnalysisRunDetails(
    Guid Id,
    Guid AnalyzedGameId,
    string Name,
    GameAnalysisRunSnapshot Snapshot,
    DateTimeOffset UpdatedAt);

public sealed record GameAnalysisRunSnapshot
{
    public GameAnalysisMode AnalysisMode { get; init; } = GameAnalysisMode.SelectedEngine;
    public int MoveCount { get; init; }
    public string? MoveListFingerprint { get; init; }
    public int ThinkTimePerMoveMs { get; init; }
    public int AnalysisThreads { get; init; }
    public IReadOnlyList<GameAnalysisEngineSnapshot> Engines { get; init; } = [];
}

public static class GameAnalysisRunFingerprint
{
    public static string Create(ChessGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var builder = new StringBuilder();
        builder.Append(FenSerializer.Serialize(game.InitialPosition));
        foreach (var moveInfo in game.Moves)
        {
            builder.Append('|');
            if (moveInfo.Move is not null)
                builder.Append(Uci.GetUci(moveInfo.Move));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    public static bool Matches(GameAnalysisRunSnapshot snapshot, ChessGame game)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(game);

        return string.IsNullOrWhiteSpace(snapshot.MoveListFingerprint)
            ? snapshot.MoveCount == game.Moves.Count
            : string.Equals(snapshot.MoveListFingerprint, Create(game), StringComparison.Ordinal);
    }
}

public sealed record GameAnalysisEngineSnapshot
{
    public Guid EngineId { get; init; }
    public string EngineName { get; init; } = string.Empty;
    public string EngineType { get; init; } = string.Empty;
    public string BinaryPath { get; init; } = string.Empty;
    public IReadOnlyList<GameAnalysisMoveEvaluationSnapshot> Evaluations { get; init; } = [];
}

public sealed record GameAnalysisMoveEvaluationSnapshot
{
    public int MoveNumber { get; init; }
    public int Score { get; init; }
    public int? Mate { get; init; }
    public int Depth { get; init; }
    public IReadOnlyList<string> PvUciMoves { get; init; } = [];
}
