using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.Board;
using pax.BlazorChess.Db;
using pax.BlazorChess.Db.Entities;
using pax.BlazorChess.Board.Storage;
using pax.chess;
using pax.chess.Analyze;
using pax.chess.Extensions;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Tests;

[TestClass]
public class EfChessBoardRepositoryTests
{
    [TestMethod]
    public async Task Save_and_load_keeps_main_line_length()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var board = new AnalysisBoard(PgnSerializer.Parse("1. d4 d5 2. c4 c6"));
        var originalLength = GetMainLineCount(board.Root);

        var id = await repo.SaveAnalyzedGame("Queen's Gambit", board);
        var loaded = await repo.LoadAnalyzedGame(id);

        Assert.IsNotNull(loaded, "Loaded analysis should not be null.");
        Assert.AreEqual(originalLength, GetMainLineCount(loaded!.Root), "Main line length changed after roundtrip.");
    }

    [TestMethod]
    public async Task Save_and_load_long_main_line_exceeding_recursive_json_depth()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var board = CreateRepeatedKnightBoard(fullMoves: 140);
        var originalLength = GetMainLineCount(board.Root);

        var id = await repo.SaveAnalyzedGame("Long knight shuffle", board);
        var loaded = await repo.LoadAnalyzedGame(id);

        Assert.AreEqual(280, originalLength);
        Assert.IsNotNull(loaded, "Loaded analysis should not be null.");
        Assert.AreEqual(originalLength, GetMainLineCount(loaded!.Root), "Main line length changed after roundtrip.");
    }

    [TestMethod]
    public async Task Save_and_load_details_roundtrips_metadata_and_analysis_tree()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var board = new AnalysisBoard(PgnSerializer.Parse("1. e4 e5 2. Nf3 Nc6"));
        var metadata = new AnalyzedGameMetadata
        {
            Event = "Spring Open",
            Site = "Berlin",
            Date = "2026.06.07",
            Round = "4",
            White = "Alpha",
            Black = "Beta",
            Result = "1-0"
        };

        var id = await repo.SaveAnalyzedGame("Alpha vs Beta", board, metadata: metadata);
        var loaded = await repo.LoadAnalyzedGameDetails(id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(id, loaded!.Id);
        Assert.AreEqual("Alpha vs Beta", loaded.Name);
        Assert.AreEqual("Spring Open", loaded.Metadata.Event);
        Assert.AreEqual("Berlin", loaded.Metadata.Site);
        Assert.AreEqual("2026.06.07", loaded.Metadata.Date);
        Assert.AreEqual("4", loaded.Metadata.Round);
        Assert.AreEqual("Alpha", loaded.Metadata.White);
        Assert.AreEqual("Beta", loaded.Metadata.Black);
        Assert.AreEqual("1-0", loaded.Metadata.Result);
        Assert.AreEqual(GetMainLineCount(board.Root), GetMainLineCount(loaded.AnalysisBoard.Root));
    }

    [TestMethod]
    public async Task Save_existing_analysis_updates_row_without_creating_duplicate()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var board = new AnalysisBoard(PgnSerializer.Parse("1. d4 d5"));
        var id = await repo.SaveAnalyzedGame("Original", board, metadata: new AnalyzedGameMetadata { Event = "First event" });
        var originalUpdatedAt = (await repo.LoadAnalyzedGameDetails(id))!.UpdatedAt;

        await Task.Delay(20);
        await repo.SaveAnalyzedGame("Updated", board, id, new AnalyzedGameMetadata { Event = "Second event" });

        var rows = await harness.Context.AnalyzedGames.AsNoTracking().ToListAsync();
        var loaded = await repo.LoadAnalyzedGameDetails(id);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Updated", loaded?.Name);
        Assert.AreEqual("Second event", loaded?.Metadata.Event);
        Assert.IsTrue(loaded!.UpdatedAt > originalUpdatedAt);
    }

    [TestMethod]
    public async Task Store_engine_options_updates_existing_rows_and_deletes_removed_rows()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var stockfish = new EngineRunOptions
        {
            Name = "Stockfish",
            BinaryPath = "stockfish.exe",
            Threads = 4,
            Pvs = 2,
            PoolSize = 4
        };
        var lc0 = new EngineRunOptions
        {
            Name = "LC0",
            BinaryPath = "lc0.exe",
            EngineType = EngineRunOptions.UciWithWeightsEngineType,
            WeightsPath = "weights.pb.gz",
            ExtraOptions = "Backend=cuda",
            IsEnabled = false,
            Threads = 2,
            Pvs = 1,
            PoolSize = 2
        };

        await repo.StoreEngineRunOptions([stockfish, lc0]);

        var firstRows = await harness.Context.EngineRunOptions
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();
        var originalCreatedAt = firstRows.Single(e => e.Id == stockfish.Id).CreatedAt;

        stockfish.Name = "Stockfish tuned";
        stockfish.Threads = 8;

        await repo.StoreEngineRunOptions([stockfish]);

        var rows = await harness.Context.EngineRunOptions
            .AsNoTracking()
            .ToListAsync();

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(stockfish.Id, rows[0].Id);
        Assert.AreEqual("Stockfish tuned", rows[0].Name);
        Assert.AreEqual(8, rows[0].Threads);
        Assert.AreEqual(originalCreatedAt, rows[0].CreatedAt);
    }

    [TestMethod]
    public async Task Store_engine_options_roundtrips_multi_engine_fields()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var lc0 = new EngineRunOptions
        {
            Name = "LCZero",
            BinaryPath = "lc0.exe",
            EngineType = EngineRunOptions.UciWithWeightsEngineType,
            WeightsPath = "network.pb.gz",
            ExtraOptions = "Backend=cuda\nMinibatchSize=256",
            IsEnabled = false,
            Threads = 2,
            Pvs = 2,
            PoolSize = 1
        };

        await repo.StoreEngineRunOptions([lc0]);

        var loaded = (await repo.GetEngineRunOptions()).Single();

        Assert.AreEqual(lc0.Id, loaded.Id);
        Assert.AreEqual(EngineRunOptions.UciWithWeightsEngineType, loaded.EngineType);
        Assert.AreEqual("network.pb.gz", loaded.WeightsPath);
        Assert.AreEqual("Backend=cuda\nMinibatchSize=256", loaded.ExtraOptions);
        Assert.IsFalse(loaded.IsEnabled);
    }

    [TestMethod]
    public async Task List_analyzed_games_returns_most_recent_summaries_first()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;

        var first = new AnalysisBoard(PgnSerializer.Parse("1. e4 e5"));
        var second = new AnalysisBoard(PgnSerializer.Parse("1. d4 d5"));

        await repo.SaveAnalyzedGame("First", first);
        await Task.Delay(20);
        await repo.SaveAnalyzedGame("Second", second);

        var summaries = await repo.ListAnalyzedGames();

        Assert.AreEqual(2, summaries.Count);
        Assert.AreEqual("Second", summaries[0].Name);
        Assert.AreEqual("First", summaries[1].Name);
    }

    [TestMethod]
    public async Task List_analyzed_games_does_not_deserialize_analysis_json()
    {
        await using var harness = await RepositoryHarness.Create();

        harness.Context.AnalyzedGames.Add(new AnalyzedGameEntity
        {
            Id = Guid.NewGuid(),
            Name = "Invalid JSON row",
            InitialFen = "invalid",
            AnalysisJson = "not-json",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await harness.Context.SaveChangesAsync();

        var summaries = await harness.Repository.ListAnalyzedGames();

        Assert.AreEqual(1, summaries.Count);
        Assert.AreEqual("Invalid JSON row", summaries[0].Name);
    }

    [TestMethod]
    public async Task Save_and_load_multiple_game_analysis_runs_per_game()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;
        var gameId = await repo.SaveAnalyzedGame("Game", new AnalysisBoard(PgnSerializer.Parse("1. e4 e5")));
        var firstSnapshot = CreateRunSnapshot("Stockfish", 42);
        var secondSnapshot = CreateRunSnapshot("LC0", -18);

        var firstRunId = await repo.SaveAnalyzedGameAnalysisRun(gameId, "Stockfish 1s", firstSnapshot);
        await Task.Delay(20);
        var secondRunId = await repo.SaveAnalyzedGameAnalysisRun(gameId, "LC0 1s", secondSnapshot);

        var summaries = await repo.ListAnalyzedGameAnalysisRuns(gameId);
        var firstRun = await repo.LoadAnalyzedGameAnalysisRun(firstRunId);
        var secondRun = await repo.LoadAnalyzedGameAnalysisRun(secondRunId);

        Assert.AreEqual(2, summaries.Count);
        Assert.AreEqual("LC0 1s", summaries[0].Name);
        Assert.AreEqual("Stockfish 1s", summaries[1].Name);
        Assert.AreEqual("Stockfish", firstRun?.Snapshot.Engines[0].EngineName);
        Assert.AreEqual(42, firstRun?.Snapshot.Engines[0].Evaluations[0].Score);
        Assert.AreEqual("LC0", secondRun?.Snapshot.Engines[0].EngineName);
        Assert.AreEqual(-18, secondRun?.Snapshot.Engines[0].Evaluations[0].Score);
    }

    [TestMethod]
    public async Task Save_existing_game_analysis_run_updates_row_without_duplicate()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;
        var gameId = await repo.SaveAnalyzedGame("Game", new AnalysisBoard(PgnSerializer.Parse("1. d4 d5")));
        var runId = await repo.SaveAnalyzedGameAnalysisRun(gameId, "Original", CreateRunSnapshot("Stockfish", 10));

        await repo.SaveAnalyzedGameAnalysisRun(gameId, "Updated", CreateRunSnapshot("Stockfish", 25), runId);

        var rows = await harness.Context.AnalyzedGameAnalysisRuns.AsNoTracking().ToListAsync();
        var loaded = await repo.LoadAnalyzedGameAnalysisRun(runId);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Updated", loaded?.Name);
        Assert.AreEqual(25, loaded?.Snapshot.Engines[0].Evaluations[0].Score);
    }

    [TestMethod]
    public async Task Delete_game_analysis_run_removes_only_selected_row()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;
        var gameId = await repo.SaveAnalyzedGame("Game", new AnalysisBoard(PgnSerializer.Parse("1. e4 e5")));
        var firstRunId = await repo.SaveAnalyzedGameAnalysisRun(gameId, "First", CreateRunSnapshot("Stockfish", 10));
        var secondRunId = await repo.SaveAnalyzedGameAnalysisRun(gameId, "Second", CreateRunSnapshot("LC0", -10));

        await repo.DeleteAnalyzedGameAnalysisRun(firstRunId);

        var summaries = await repo.ListAnalyzedGameAnalysisRuns(gameId);
        var deleted = await repo.LoadAnalyzedGameAnalysisRun(firstRunId);
        var remaining = await repo.LoadAnalyzedGameAnalysisRun(secondRunId);

        Assert.AreEqual(1, summaries.Count);
        Assert.AreEqual(secondRunId, summaries[0].Id);
        Assert.IsNull(deleted);
        Assert.IsNotNull(remaining);
    }

    [TestMethod]
    public async Task Delete_analyzed_game_cascades_analysis_runs()
    {
        await using var harness = await RepositoryHarness.Create();
        var repo = harness.Repository;
        var gameId = await repo.SaveAnalyzedGame("Game", new AnalysisBoard(PgnSerializer.Parse("1. e4 e5")));
        await repo.SaveAnalyzedGameAnalysisRun(gameId, "First", CreateRunSnapshot("Stockfish", 10));
        await repo.SaveAnalyzedGameAnalysisRun(gameId, "Second", CreateRunSnapshot("LC0", -10));

        await repo.DeleteAnalyzedGame(gameId);

        var game = await repo.LoadAnalyzedGame(gameId);
        var runs = await harness.Context.AnalyzedGameAnalysisRuns.AsNoTracking().ToListAsync();
        Assert.IsNull(game);
        Assert.AreEqual(0, runs.Count);
    }

    [TestMethod]
    public async Task List_game_analysis_runs_does_not_deserialize_analysis_json()
    {
        await using var harness = await RepositoryHarness.Create();
        var gameId = await harness.Repository.SaveAnalyzedGame("Game", new AnalysisBoard(PgnSerializer.Parse("1. c4 e5")));

        harness.Context.AnalyzedGameAnalysisRuns.Add(new AnalyzedGameAnalysisRunEntity
        {
            Id = Guid.NewGuid(),
            AnalyzedGameId = gameId,
            Name = "Invalid run JSON",
            AnalysisJson = "not-json",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await harness.Context.SaveChangesAsync();

        var summaries = await harness.Repository.ListAnalyzedGameAnalysisRuns(gameId);

        Assert.AreEqual(1, summaries.Count);
        Assert.AreEqual("Invalid run JSON", summaries[0].Name);
    }

    private static int GetMainLineCount(MoveNode root)
    {
        int count = 0;
        var current = root.MainLine;
        while (current != null)
        {
            count++;
            current = current.MainLine;
        }
        return count;
    }

    private static GameAnalysisRunSnapshot CreateRunSnapshot(string engineName, int score)
        => new()
        {
            AnalysisMode = GameAnalysisMode.SelectedEngine,
            MoveCount = 2,
            ThinkTimePerMoveMs = 1000,
            AnalysisThreads = 4,
            Engines =
            [
                new GameAnalysisEngineSnapshot
                {
                    EngineId = Guid.NewGuid(),
                    EngineName = engineName,
                    EngineType = EngineRunOptions.UciEngineType,
                    BinaryPath = $"{engineName}.exe",
                    Evaluations =
                    [
                        new GameAnalysisMoveEvaluationSnapshot
                        {
                            MoveNumber = 1,
                            Score = score,
                            Depth = 16,
                            PvUciMoves = ["e2e4", "e7e5"]
                        }
                    ]
                }
            ]
        };

    private static AnalysisBoard CreateRepeatedKnightBoard(int fullMoves)
    {
        var board = new AnalysisBoard(new ChessGame(BoardPosition.CreateInitial()));
        for (var moveNumber = 1; moveNumber <= fullMoves; moveNumber++)
        {
            if (moveNumber % 2 == 1)
            {
                AddMove(board, "g1f3");
                AddMove(board, "g8f6");
            }
            else
            {
                AddMove(board, "f3g1");
                AddMove(board, "f6g8");
            }
        }

        return board;
    }

    private static void AddMove(AnalysisBoard board, string uci)
    {
        var move = Uci.CreateMove(uci, board.CurrentPosition)
            ?? throw new InvalidOperationException($"Could not create move '{uci}'.");

        board.AddVariation(move);
    }

    private sealed class RepositoryHarness : IAsyncDisposable
    {
        public EfChessBoardRepository Repository { get; init; } = default!;
        public ChessContext Context { get; init; } = default!;
        private SqliteConnection Connection { get; init; } = default!;

        public static async Task<RepositoryHarness> Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ChessContext>()
                .UseSqlite(connection)
                .Options;

            var ctx = new ChessContext(options);
            await ctx.Database.EnsureCreatedAsync();

            var repo = new EfChessBoardRepository(ctx);

            return new RepositoryHarness
            {
                Repository = repo,
                Context = ctx,
                Connection = connection
            };
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
