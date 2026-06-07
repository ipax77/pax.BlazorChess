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

    [TestMethod]
    public async Task Save_new_analysis_sets_current_id_and_clears_dirty_state()
    {
        var repository = new RecordingChessBoardRepository([]);
        await using var state = CreateState(repository);

        Assert.IsTrue(state.TryLoadPgn("1. e4 e5"));
        state.RenameGame("King pawn");
        state.UpdateGameMetadata(new AnalyzedGameMetadata { White = "Ada", Black = "Byron", Result = "*" });

        await state.SaveCurrentAnalysisAsync();

        Assert.IsTrue(state.CurrentAnalyzedGameId.HasValue);
        Assert.IsFalse(state.IsDirty);
        Assert.AreEqual(1, repository.SavedAnalysisCount);

        var details = await repository.LoadAnalyzedGameDetails(state.CurrentAnalyzedGameId.Value);
        Assert.IsNotNull(details);
        Assert.AreEqual("King pawn", details!.Name);
        Assert.AreEqual("Ada", details.Metadata.White);
        Assert.AreEqual("Byron", details.Metadata.Black);
    }

    [TestMethod]
    public async Task Save_existing_analysis_overwrites_current_id()
    {
        var repository = new RecordingChessBoardRepository([]);
        await using var state = CreateState(repository);

        Assert.IsTrue(state.TryLoadPgn("1. e4 e5"));
        state.RenameGame("Original");
        await state.SaveCurrentAnalysisAsync();
        var originalId = state.CurrentAnalyzedGameId;

        state.RenameGame("Updated");
        await state.SaveCurrentAnalysisAsync();

        Assert.AreEqual(originalId, state.CurrentAnalyzedGameId);
        Assert.AreEqual(1, repository.SavedAnalysisCount);

        var details = await repository.LoadAnalyzedGameDetails(originalId!.Value);
        Assert.AreEqual("Updated", details?.Name);
    }

    [TestMethod]
    public async Task Save_as_creates_new_saved_analysis()
    {
        var repository = new RecordingChessBoardRepository([]);
        await using var state = CreateState(repository);

        Assert.IsTrue(state.TryLoadPgn("1. d4 d5"));
        state.RenameGame("First");
        await state.SaveCurrentAnalysisAsync();
        var firstId = state.CurrentAnalyzedGameId;

        state.RenameGame("Copy");
        await state.SaveCurrentAnalysisAsync(saveAs: true);

        Assert.AreNotEqual(firstId, state.CurrentAnalyzedGameId);
        Assert.AreEqual(2, repository.SavedAnalysisCount);
    }

    [TestMethod]
    public async Task Load_saved_analysis_restores_board_metadata_id_and_clean_state()
    {
        var repository = new RecordingChessBoardRepository([]);
        var board = new AnalysisBoard(pax.chess.PgnSerializer.Parse("1. c4 e5 2. Nc3"));
        var id = await repository.SaveAnalyzedGame(
            "English",
            board,
            metadata: new AnalyzedGameMetadata { Event = "Club night", White = "White player" });
        await using var state = CreateState(repository);

        await state.LoadAnalyzedGameAsync(id);

        Assert.AreEqual(id, state.CurrentAnalyzedGameId);
        Assert.AreEqual("English", state.GameName);
        Assert.AreEqual("Club night", state.GameMetadata.Event);
        Assert.AreEqual("White player", state.GameMetadata.White);
        Assert.IsFalse(state.IsDirty);
        Assert.AreEqual(3, GetMainLineCount(state.AnalysisBoard.Root));
    }

    [TestMethod]
    public async Task Save_current_game_analysis_run_requires_saved_game_and_roundtrips_snapshot()
    {
        var repository = new RecordingChessBoardRepository([]);
        await using var state = CreateState(repository);
        var snapshot = CreateAnalysisRunSnapshot();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            state.SaveCurrentGameAnalysisRunAsync("Stockfish run", snapshot));

        Assert.IsTrue(state.TryLoadPgn("1. e4 e5"));
        await state.SaveCurrentAnalysisAsync();

        var runId = await state.SaveCurrentGameAnalysisRunAsync("Stockfish run", snapshot);
        var summaries = await state.ListCurrentGameAnalysisRunsAsync();
        var details = await state.LoadGameAnalysisRunAsync(runId);

        Assert.AreEqual(1, summaries.Count);
        Assert.AreEqual("Stockfish run", summaries[0].Name);
        Assert.IsNotNull(details);
        Assert.AreEqual(runId, details!.Id);
        Assert.AreEqual(state.CurrentAnalyzedGameId, details.AnalyzedGameId);
        Assert.AreEqual(1, details.Snapshot.Engines.Count);
        Assert.AreEqual(2, details.Snapshot.Engines[0].Evaluations.Count);
    }

    [TestMethod]
    public async Task Pgn_tag_import_prefills_game_metadata()
    {
        var repository = new RecordingChessBoardRepository([]);
        await using var state = CreateState(repository);

        var loaded = state.TryLoadPgn("""
            [Event "Spring Open"]
            [Site "Berlin"]
            [Date "2026.06.07"]
            [Round "4"]
            [White "Alpha"]
            [Black "Beta"]
            [Result "1-0"]

            1. e4 e5 2. Qh5 Nc6
            """);

        Assert.IsTrue(loaded, state.ImportError);
        Assert.AreEqual("Spring Open", state.GameMetadata.Event);
        Assert.AreEqual("Berlin", state.GameMetadata.Site);
        Assert.AreEqual("2026.06.07", state.GameMetadata.Date);
        Assert.AreEqual("4", state.GameMetadata.Round);
        Assert.AreEqual("Alpha", state.GameMetadata.White);
        Assert.AreEqual("Beta", state.GameMetadata.Black);
        Assert.AreEqual("1-0", state.GameMetadata.Result);
        Assert.AreEqual("Alpha vs Beta", state.GameName);
        Assert.IsNull(state.CurrentAnalyzedGameId);
        Assert.IsTrue(state.IsDirty);
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

    private static int GetMainLineCount(MoveNode root)
    {
        var count = 0;
        var current = root.MainLine;
        while (current is not null)
        {
            count++;
            current = current.MainLine;
        }

        return count;
    }

    private static GameAnalysisRunSnapshot CreateAnalysisRunSnapshot()
        => new()
        {
            AnalysisMode = pax.BlazorChess.Board.GameAnalysisMode.SelectedEngine,
            MoveCount = 2,
            ThinkTimePerMoveMs = 1000,
            AnalysisThreads = 4,
            Engines =
            [
                new GameAnalysisEngineSnapshot
                {
                    EngineId = Guid.NewGuid(),
                    EngineName = "Stockfish",
                    EngineType = EngineRunOptions.UciEngineType,
                    BinaryPath = "stockfish.exe",
                    Evaluations =
                    [
                        new GameAnalysisMoveEvaluationSnapshot
                        {
                            MoveNumber = 1,
                            Score = 32,
                            Depth = 18,
                            PvUciMoves = ["e2e4", "e7e5"]
                        },
                        new GameAnalysisMoveEvaluationSnapshot
                        {
                            MoveNumber = 2,
                            Score = -12,
                            Depth = 18,
                            PvUciMoves = ["e7e5", "g1f3"]
                        }
                    ]
                }
            ]
        };

    private sealed class RecordingChessBoardRepository : IChessBoardRepository
    {
        private readonly List<EngineRunOptions> _engineRunOptions;
        private readonly Dictionary<Guid, (string Name, string Json, AnalyzedGameMetadata Metadata, DateTimeOffset UpdatedAt)> _analyses = new();
        private readonly Dictionary<Guid, (Guid AnalyzedGameId, string Name, string Json, DateTimeOffset UpdatedAt)> _analysisRuns = new();
        private readonly TaskCompletionSource _storeCompleted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();

        public RecordingChessBoardRepository(List<EngineRunOptions> engineRunOptions)
        {
            _engineRunOptions = engineRunOptions;
        }

        public int StoreCount { get; private set; }
        public int SavedAnalysisCount => _analyses.Count;
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
        {
            IReadOnlyList<AnalyzedGameSummary> summaries = _analyses
                .Select(a => new AnalyzedGameSummary(a.Key, a.Value.Name, a.Value.UpdatedAt))
                .OrderByDescending(a => a.UpdatedAt)
                .ToList();

            return Task.FromResult(summaries);
        }

        public Task<AnalysisBoard?> LoadAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_analyses.TryGetValue(id, out var saved))
                return Task.FromResult<AnalysisBoard?>(null);

            return Task.FromResult<AnalysisBoard?>(AnalysisSerializer.Restore(saved.Json));
        }

        public Task<AnalyzedGameDetails?> LoadAnalyzedGameDetails(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_analyses.TryGetValue(id, out var saved))
                return Task.FromResult<AnalyzedGameDetails?>(null);

            var details = new AnalyzedGameDetails(
                id,
                saved.Name,
                AnalysisSerializer.Restore(saved.Json),
                saved.Metadata,
                saved.UpdatedAt);

            return Task.FromResult<AnalyzedGameDetails?>(details);
        }

        public Task<Guid> SaveAnalyzedGame(
            string name,
            AnalysisBoard analysisBoard,
            Guid? id = default,
            AnalyzedGameMetadata? metadata = default,
            CancellationToken cancellationToken = default)
        {
            var targetId = id ?? Guid.NewGuid();
            _analyses[targetId] = (name, AnalysisSerializer.Serialize(analysisBoard), metadata ?? AnalyzedGameMetadata.Empty, DateTimeOffset.UtcNow);
            return Task.FromResult(targetId);
        }

        public Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AnalyzedGameAnalysisRunSummary>> ListAnalyzedGameAnalysisRuns(
            Guid analyzedGameId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AnalyzedGameAnalysisRunSummary> summaries = _analysisRuns
                .Where(r => r.Value.AnalyzedGameId == analyzedGameId)
                .Select(r => new AnalyzedGameAnalysisRunSummary(r.Key, r.Value.AnalyzedGameId, r.Value.Name, r.Value.UpdatedAt))
                .OrderByDescending(r => r.UpdatedAt)
                .ToList();

            return Task.FromResult(summaries);
        }

        public Task<AnalyzedGameAnalysisRunDetails?> LoadAnalyzedGameAnalysisRun(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!_analysisRuns.TryGetValue(id, out var saved))
                return Task.FromResult<AnalyzedGameAnalysisRunDetails?>(null);

            var details = new AnalyzedGameAnalysisRunDetails(
                id,
                saved.AnalyzedGameId,
                saved.Name,
                GameAnalysisRunSerializer.Restore(saved.Json),
                saved.UpdatedAt);

            return Task.FromResult<AnalyzedGameAnalysisRunDetails?>(details);
        }

        public Task<Guid> SaveAnalyzedGameAnalysisRun(
            Guid analyzedGameId,
            string name,
            GameAnalysisRunSnapshot snapshot,
            Guid? id = default,
            CancellationToken cancellationToken = default)
        {
            var targetId = id ?? Guid.NewGuid();
            _analysisRuns[targetId] = (analyzedGameId, name, GameAnalysisRunSerializer.Serialize(snapshot), DateTimeOffset.UtcNow);
            return Task.FromResult(targetId);
        }
    }
}
