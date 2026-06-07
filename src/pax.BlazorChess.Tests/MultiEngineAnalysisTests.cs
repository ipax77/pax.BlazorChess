using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.AnalysisWeb.Services;
using pax.chess;
using pax.chess.Analyze;
using pax.chess.Extensions;
using pax.uciChessEngine;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Tests;

[TestClass]
public sealed class MultiEngineAnalysisTests
{
    [TestMethod]
    public void Comparison_builder_unions_candidates_and_marks_ranks()
    {
        var board = new AnalysisBoard(new ChessGame());
        var position = board.CurrentPosition;
        var stockfish = new EngineRunOptions { Name = "Stockfish" };
        var lc0 = new EngineRunOptions { Name = "LCZero" };
        var ethereal = new EngineRunOptions { Name = "Ethereal" };

        var snapshots = new[]
        {
            Snapshot(stockfish, [
                Line(1, "+0.4", "e2e4", "e4", board.Root, position),
                Line(2, "+0.2", "d2d4", "d4", board.Root, position)
            ]),
            Snapshot(lc0, [
                Line(1, "+0.3", "e2e4", "e4", board.Root, position)
            ]),
            Snapshot(ethereal, [
                Line(1, "+0.1", "d2d4", "d4", board.Root, position)
            ])
        };

        var comparison = EngineComparisonBuilder.Build(snapshots);

        Assert.AreEqual("e4", comparison.ConsensusBestMove);
        Assert.IsTrue(comparison.HasDisagreement);
        Assert.AreEqual(2, comparison.CandidateRows.Count);

        var e4 = comparison.CandidateRows.Single(r => r.Uci == "e2e4");
        Assert.AreEqual("best", e4.Cells[stockfish.Id].RankText);
        Assert.AreEqual("best", e4.Cells[lc0.Id].RankText);
        Assert.IsFalse(e4.Cells.ContainsKey(ethereal.Id));

        var d4 = comparison.CandidateRows.Single(r => r.Uci == "d2d4");
        Assert.AreEqual("#2", d4.Cells[stockfish.Id].RankText);
        Assert.AreEqual("best", d4.Cells[ethereal.Id].RankText);
    }

    [TestMethod]
    public async Task Coordinator_restart_clears_stale_snapshots_without_starting_old_run()
    {
        await using var coordinator = new MultiEngineAnalysisCoordinator();
        var board = new AnalysisBoard(new ChessGame());
        var engine = new EngineRunOptions
        {
            Name = "Missing",
            BinaryPath = "missing-engine.exe",
            IsEnabled = true
        };

        await coordinator.StartAsync(board, [engine], maxParallelEngines: 1);
        engine.IsEnabled = false;
        await coordinator.StartAsync(board, [engine], maxParallelEngines: 1);
        await Task.Delay(300);

        var snapshot = coordinator.Snapshots.Single();

        Assert.AreEqual(EngineAnalysisStatus.Disabled, snapshot.Status);
        Assert.AreEqual(string.Empty, snapshot.Error);
    }

    [TestMethod]
    public void Build_lines_ignores_invalid_first_pv_move()
    {
        var board = new AnalysisBoard(new ChessGame());
        var evals = new List<Eval>
        {
            Eval(1, 12, ["a3a4"])
        };

        var lines = MultiEngineAnalysisCoordinator.BuildLines(
            evals,
            board.Root,
            board.CurrentPosition);

        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Build_lines_ignores_empty_and_no_move_pvs()
    {
        var board = new AnalysisBoard(new ChessGame());
        var evals = new List<Eval>
        {
            Eval(1, 12, []),
            Eval(2, 12, ["(none)"])
        };

        var lines = MultiEngineAnalysisCoordinator.BuildLines(
            evals,
            board.Root,
            board.CurrentPosition);

        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Build_lines_preserves_legal_pv_snapshots()
    {
        var board = new AnalysisBoard(new ChessGame());
        var evals = new List<Eval>
        {
            Eval(1, 12, ["e2e4", "e7e5"])
        };

        var lines = MultiEngineAnalysisCoordinator.BuildLines(
            evals,
            board.Root,
            board.CurrentPosition);

        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(2, lines[0].Moves.Count);
        CollectionAssert.AreEqual(new[] { "e2e4", "e7e5" }, lines[0].UciMoves.ToArray());
        Assert.AreEqual("e4", lines[0].Moves[0].San);
        Assert.AreEqual("e5", lines[0].Moves[1].San);
    }

    [TestMethod]
    public void Build_lines_trims_invalid_pv_tail()
    {
        var board = new AnalysisBoard(new ChessGame());
        var evals = new List<Eval>
        {
            Eval(1, 12, ["e2e4", "a3a4"])
        };

        var lines = MultiEngineAnalysisCoordinator.BuildLines(
            evals,
            board.Root,
            board.CurrentPosition);

        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(1, lines[0].Moves.Count);
        CollectionAssert.AreEqual(new[] { "e2e4" }, lines[0].UciMoves.ToArray());
    }

    [TestMethod]
    public void Build_lines_returns_empty_for_empty_evals()
    {
        var board = new AnalysisBoard(new ChessGame());

        var lines = MultiEngineAnalysisCoordinator.BuildLines(
            [],
            board.Root,
            board.CurrentPosition);

        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Game_analysis_does_not_add_variation_after_final_move()
    {
        var board = CreateBoardAtEnd("1. e4 e5");
        var originalMainLineCount = CountMainLineMoves(board.Root);
#pragma warning disable BL0005
        var component = new pax.BlazorChess.Board.GameAnalysisComponent
        {
            ChessGame = board.ChessGame,
            AnalysisBoard = board,
            EngineRunOptions = new EngineRunOptions()
        };
#pragma warning restore BL0005

        var method = typeof(pax.BlazorChess.Board.GameAnalysisComponent)
            .GetMethod("AddVariationToAnalysisBoard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(pax.BlazorChess.Board.GameAnalysisComponent), "AddVariationToAnalysisBoard");

        var changed = (bool)method.Invoke(component, [originalMainLineCount, Eval(1, 12, ["g1f3"]), "Final move"])!;

        Assert.IsFalse(changed);
        Assert.AreEqual(originalMainLineCount, CountMainLineMoves(board.Root));
    }

    [TestMethod]
    public void Game_analysis_does_not_extend_main_line_when_pv_starts_with_next_game_move()
    {
        var board = CreateBoardAtEnd("1. e4 e5");
        var originalMainLineCount = CountMainLineMoves(board.Root);
#pragma warning disable BL0005
        var component = new pax.BlazorChess.Board.GameAnalysisComponent
        {
            ChessGame = board.ChessGame,
            AnalysisBoard = board,
            EngineRunOptions = new EngineRunOptions()
        };
#pragma warning restore BL0005

        var method = typeof(pax.BlazorChess.Board.GameAnalysisComponent)
            .GetMethod("AddVariationToAnalysisBoard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(pax.BlazorChess.Board.GameAnalysisComponent), "AddVariationToAnalysisBoard");

        var changed = (bool)method.Invoke(component, [1, Eval(1, 12, ["e7e5", "g1f3"]), "Continuation"])!;

        Assert.IsFalse(changed);
        Assert.AreEqual(originalMainLineCount, CountMainLineMoves(board.Root));
    }

    private static EngineAnalysisSnapshot Snapshot(
        EngineRunOptions engine,
        IReadOnlyList<EnginePvLineSnapshot> lines)
        => EngineAnalysisSnapshot.FromOptions(
            engine,
            EngineAnalysisStatus.Running,
            "Depth 10",
            lines: lines);

    private static EnginePvLineSnapshot Line(
        int multiPv,
        string score,
        string uci,
        string san,
        MoveNode node,
        BoardPosition position)
    {
        var move = Uci.CreateMove(uci, position) ?? throw new InvalidOperationException($"Invalid move {uci}.");
        return new EnginePvLineSnapshot(
            multiPv,
            score,
            0,
            null,
            10,
            node,
            [uci],
            [new EnginePvMoveSnapshot(uci, san, move)]);
    }

    private static Eval Eval(int multiPv, int depth, ICollection<string> moves)
    {
        var values = new Dictionary<string, int>
        {
            ["multipv"] = multiPv,
            ["depth"] = depth,
            ["cp"] = 0
        };
        var pvInfo = new pax.uciChessEngine.PvInfo(multiPv, values, moves);

        return new Eval
        {
            Score = pvInfo.Score,
            Mate = pvInfo.Mate,
            Depth = pvInfo.Depth,
            PvInfo = pvInfo
        };
    }

    private static AnalysisBoard CreateBoardAtEnd(string pgn)
    {
        var board = new AnalysisBoard(PgnSerializer.Parse(pgn));
        var current = board.Root;
        while (current.MainLine is not null)
            current = current.MainLine;

        board.MoveToNode(current);
        return board;
    }

    private static int CountMainLineMoves(MoveNode root)
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

}
