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
            Threads = 4,
            Pvs = 4,
            PoolSize = 8,
        },
        new()
        {
            BinaryPath = @"C:\data\chess\engines\lc0-v0.32.1-windows-gpu-nvidia-cuda11\lc0.exe",
            Name = "LC0 v0.32.1",
            Threads = 2,
            Pvs = 2,
            PoolSize = 2,
        }
    ];

    private readonly Dictionary<Guid, (string Name, string Json, DateTimeOffset UpdatedAt)> _analyses = new();

    public Task<List<EngineRunOptions>> GetEngineRunOptions(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_engineRunOptions.ToList());
    }

    public Task StoreEngineRunOptions(List<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default)
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

    public Task<Guid> SaveAnalyzedGame(string name, AnalysisBoard analysisBoard, Guid? id = default, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(analysisBoard);

        var snapshotJson = AnalysisSerializer.Serialize(analysisBoard);
        var targetId = id ?? Guid.NewGuid();

        _analyses[targetId] = (name, snapshotJson, DateTimeOffset.UtcNow);

        return Task.FromResult(targetId);
    }

    public Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
    {
        _analyses.Remove(id);
        return Task.CompletedTask;
    }
}
