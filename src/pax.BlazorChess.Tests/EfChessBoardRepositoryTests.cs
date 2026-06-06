using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.Db;
using pax.BlazorChess.Board.Storage;
using pax.chess;
using pax.chess.Analyze;
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
