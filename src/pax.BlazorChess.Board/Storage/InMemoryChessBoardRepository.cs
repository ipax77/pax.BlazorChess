using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Board.Storage;

/// <summary>
/// Simple in-memory repository that mirrors the EF-backed API.
/// Useful for tests and in-browser use without persistence.
/// </summary>
public sealed class InMemoryChessBoardRepository : IChessBoardRepository
{
    private readonly List<EngineRunOptions> _engineRunOptions =
    [
        new()
        {
            BinaryPath = @"C:\data\chess\engines\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe",
            Name = "Stockfish 18",
            EngineType = EngineRunOptions.UciEngineType,
            IsEnabled = true,
            Threads = 4,
            Pvs = 4,
            PoolSize = 8,
        },
        new()
        {
            BinaryPath = @"C:\data\chess\engines\lc0-v0.32.1-windows-gpu-nvidia-cuda11\lc0.exe",
            Name = "LC0 v0.32.1",
            EngineType = EngineRunOptions.UciWithWeightsEngineType,
            IsEnabled = true,
            Threads = 2,
            Pvs = 2,
            PoolSize = 2,
        }
    ];

    private readonly Dictionary<Guid, (string Name, string Json, AnalyzedGameMetadata Metadata, DateTimeOffset UpdatedAt)> _analyses = new();
    private readonly Dictionary<Guid, (Guid AnalyzedGameId, string Name, string Json, DateTimeOffset UpdatedAt)> _analysisRuns = new();

    public Task<List<EngineRunOptions>> GetEngineRunOptions(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_engineRunOptions.ToList());
    }

    public Task StoreEngineRunOptions(IReadOnlyList<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default)
    {
        _engineRunOptions.Clear();
        _engineRunOptions.AddRange(engineRunOptions);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AnalyzedGameSummary>> ListAnalyzedGames(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AnalyzedGameSummary> result = _analyses
            .Select(kvp => new AnalyzedGameSummary(kvp.Key, kvp.Value.Name, kvp.Value.UpdatedAt))
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<AnalysisBoard?> LoadAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_analyses.TryGetValue(id, out var data))
            return Task.FromResult<AnalysisBoard?>(null);

        var board = AnalysisSerializer.Restore(data.Json);
        return Task.FromResult<AnalysisBoard?>(board);
    }

    public Task<AnalyzedGameDetails?> LoadAnalyzedGameDetails(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_analyses.TryGetValue(id, out var data))
            return Task.FromResult<AnalyzedGameDetails?>(null);

        var board = AnalysisSerializer.Restore(data.Json);
        var details = new AnalyzedGameDetails(id, data.Name, board, data.Metadata, data.UpdatedAt);
        return Task.FromResult<AnalyzedGameDetails?>(details);
    }

    public Task<Guid> SaveAnalyzedGame(
        string name,
        AnalysisBoard analysisBoard,
        Guid? id = default,
        AnalyzedGameMetadata? metadata = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(analysisBoard);

        var snapshotJson = AnalysisSerializer.Serialize(analysisBoard);
        var targetId = id ?? Guid.NewGuid();

        _analyses[targetId] = (name, snapshotJson, Normalize(metadata), DateTimeOffset.UtcNow);

        return Task.FromResult(targetId);
    }

    public Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
    {
        _analyses.Remove(id);
        foreach (var runId in _analysisRuns
            .Where(r => r.Value.AnalyzedGameId == id)
            .Select(r => r.Key)
            .ToList())
        {
            _analysisRuns.Remove(runId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AnalyzedGameAnalysisRunSummary>> ListAnalyzedGameAnalysisRuns(
        Guid analyzedGameId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AnalyzedGameAnalysisRunSummary> result = _analysisRuns
            .Where(r => r.Value.AnalyzedGameId == analyzedGameId)
            .Select(r => new AnalyzedGameAnalysisRunSummary(r.Key, r.Value.AnalyzedGameId, r.Value.Name, r.Value.UpdatedAt))
            .OrderByDescending(r => r.UpdatedAt)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<AnalyzedGameAnalysisRunDetails?> LoadAnalyzedGameAnalysisRun(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!_analysisRuns.TryGetValue(id, out var data))
            return Task.FromResult<AnalyzedGameAnalysisRunDetails?>(null);

        var details = new AnalyzedGameAnalysisRunDetails(
            id,
            data.AnalyzedGameId,
            data.Name,
            GameAnalysisRunSerializer.Restore(data.Json),
            data.UpdatedAt);

        return Task.FromResult<AnalyzedGameAnalysisRunDetails?>(details);
    }

    public Task<Guid> SaveAnalyzedGameAnalysisRun(
        Guid analyzedGameId,
        string name,
        GameAnalysisRunSnapshot snapshot,
        Guid? id = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(snapshot);

        var targetId = id ?? Guid.NewGuid();
        _analysisRuns[targetId] = (
            analyzedGameId,
            string.IsNullOrWhiteSpace(name) ? "Analysis" : name.Trim(),
            GameAnalysisRunSerializer.Serialize(snapshot),
            DateTimeOffset.UtcNow);

        return Task.FromResult(targetId);
    }

    public Task DeleteAnalyzedGameAnalysisRun(Guid id, CancellationToken cancellationToken = default)
    {
        _analysisRuns.Remove(id);
        return Task.CompletedTask;
    }

    private static AnalyzedGameMetadata Normalize(AnalyzedGameMetadata? metadata)
    {
        var value = metadata ?? AnalyzedGameMetadata.Empty;
        return new()
        {
            Event = Normalize(value.Event),
            Site = Normalize(value.Site),
            Date = Normalize(value.Date),
            Round = Normalize(value.Round),
            White = Normalize(value.White),
            Black = Normalize(value.Black),
            Result = Normalize(value.Result)
        };
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
