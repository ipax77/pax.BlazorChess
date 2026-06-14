using pax.chess;
using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Maui.Services;

public enum EngineAnalysisStatus
{
    Disabled,
    Paused,
    Queued,
    Starting,
    Running,
    Ready,
    Error
}

public sealed record EnginePvMoveSnapshot(string Uci, string San, Move Move);

public sealed record EnginePvLineSnapshot(
    int MultiPv,
    string ScoreText,
    int Score,
    int? Mate,
    int Depth,
    MoveNode BaseNode,
    IReadOnlyList<string> UciMoves,
    IReadOnlyList<EnginePvMoveSnapshot> Moves);

public sealed record EngineAnalysisSnapshot(
    Guid EngineId,
    string EngineName,
    string EngineType,
    string BinaryPath,
    bool IsEnabled,
    EngineAnalysisStatus Status,
    string StatusText,
    string Error,
    IReadOnlyList<EnginePvLineSnapshot> Lines)
{
    public static EngineAnalysisSnapshot FromOptions(
        EngineRunOptions options,
        EngineAnalysisStatus status,
        string statusText,
        string? error = null,
        IReadOnlyList<EnginePvLineSnapshot>? lines = null)
        => new(
            options.Id,
            string.IsNullOrWhiteSpace(options.Name) ? "Unnamed engine" : options.Name!,
            string.IsNullOrWhiteSpace(options.EngineType) ? EngineRunOptions.UciEngineType : options.EngineType,
            options.BinaryPath,
            options.IsEnabled,
            status,
            statusText,
            error ?? string.Empty,
            lines ?? []);
}

public sealed record CandidateMoveCell(string EvalText, string RankText, int Rank);

public sealed record CandidateMoveRow(
    string Uci,
    string San,
    IReadOnlyDictionary<Guid, CandidateMoveCell> Cells);

public sealed record EngineComparisonSnapshot(
    IReadOnlyList<CandidateMoveRow> CandidateRows,
    string ConsensusBestMove,
    bool HasDisagreement);
