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
}
