using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.Db;
using pax.BlazorChess.Board.Storage;
using pax.chess;
using pax.chess.Analyze;

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
        private ChessContext Context { get; init; } = default!;
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
