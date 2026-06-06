using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.AnalysisWeb.Services;
using pax.BlazorChess.Board.Storage;
using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Tests;

[TestClass]
public sealed class AnalysisWorkspaceStatePersistenceTests
{
    [TestMethod]
    public async Task Load_engine_settings_populates_workspace_and_selects_first_engine()
    {
        var stockfish = new EngineRunOptions
        {
            Name = "Stockfish",
            BinaryPath = "stockfish.exe",
            Threads = 4,
            Pvs = 3,
            HashMb = 64,
            PoolSize = 2
        };
        var lc0 = new EngineRunOptions
        {
            Name = "LC0",
            BinaryPath = "lc0.exe",
            EngineType = EngineRunOptions.UciWithWeightsEngineType,
            WeightsPath = "lc0.pb.gz",
            ExtraOptions = "Backend=cuda",
            IsEnabled = false,
            Threads = 2,
            Pvs = 1,
            HashMb = 32,
            PoolSize = 1
        };

        var repository = new RecordingChessBoardRepository([stockfish, lc0]);
        await using var state = CreateState(repository);

        await state.LoadEngineSettingsAsync();

        Assert.AreEqual(2, state.EngineRunOptions.Count);
        Assert.AreEqual(stockfish.Id, state.SelectedEngineId);
        Assert.AreEqual("Stockfish", state.SelectedEngine?.Name);
        Assert.AreEqual("stockfish.exe", state.SelectedEngine?.BinaryPath);
        Assert.AreEqual(EngineRunOptions.UciWithWeightsEngineType, state.EngineRunOptions[1].EngineType);
        Assert.AreEqual("lc0.pb.gz", state.EngineRunOptions[1].WeightsPath);
        Assert.AreEqual("Backend=cuda", state.EngineRunOptions[1].ExtraOptions);
        Assert.IsFalse(state.EngineRunOptions[1].IsEnabled);
    }

    [TestMethod]
    public async Task Debounced_engine_settings_save_stores_latest_values_once()
    {
        var repository = new RecordingChessBoardRepository([]);
        await using var state = CreateState(repository, debounceMs: 50);

        var engine = state.AddEngine();
        engine.BinaryPath = "first.exe";
        state.ScheduleEngineSettingsSave();

        engine.BinaryPath = "second.exe";
        engine.Threads = 6;
        state.ScheduleEngineSettingsSave();

        engine.Name = "Final engine";
        state.ScheduleEngineSettingsSave();

        await repository.WaitForStoreAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(1, repository.StoreCount);
        Assert.AreEqual(1, repository.LastStored.Count);
        Assert.AreEqual(engine.Id, repository.LastStored[0].Id);
        Assert.AreEqual("Final engine", repository.LastStored[0].Name);
        Assert.AreEqual("second.exe", repository.LastStored[0].BinaryPath);
        Assert.AreEqual(6, repository.LastStored[0].Threads);
    }

    private static AnalysisWorkspaceState CreateState(
        IChessBoardRepository repository,
        int debounceMs = 1)
    {
        var options = Options.Create(new AnalysisPersistenceOptions
        {
            EngineSettingsSaveDebounceMs = debounceMs
        });

        return new AnalysisWorkspaceState(repository, options);
    }

    private sealed class RecordingChessBoardRepository : IChessBoardRepository
    {
        private readonly List<EngineRunOptions> _engineRunOptions;
        private readonly TaskCompletionSource _storeCompleted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();

        public RecordingChessBoardRepository(List<EngineRunOptions> engineRunOptions)
        {
            _engineRunOptions = engineRunOptions;
        }

        public int StoreCount { get; private set; }
        public List<EngineRunOptions> LastStored { get; private set; } = [];

        public Task<List<EngineRunOptions>> GetEngineRunOptions(CancellationToken cancellationToken = default)
            => Task.FromResult(_engineRunOptions.ToList());

        public Task StoreEngineRunOptions(List<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                StoreCount++;
                LastStored = engineRunOptions.ToList();
            }

            _storeCompleted.TrySetResult();
            return Task.CompletedTask;
        }

        public async Task WaitForStoreAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_storeCompleted.Task, Task.Delay(timeout));
            Assert.AreSame(_storeCompleted.Task, completed, "Timed out waiting for debounced engine settings save.");
        }

        public Task<IReadOnlyList<AnalyzedGameSummary>> ListAnalyzedGames(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AnalyzedGameSummary>>([]);

        public Task<AnalysisBoard?> LoadAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<AnalysisBoard?>(null);

        public Task<Guid> SaveAnalyzedGame(
            string name,
            AnalysisBoard analysisBoard,
            Guid? id = default,
            CancellationToken cancellationToken = default)
            => Task.FromResult(id ?? Guid.NewGuid());

        public Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
