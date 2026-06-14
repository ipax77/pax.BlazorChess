using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.Board.Storage;
using pax.chess;
using pax.chess.Analyze;
using pax.chess.Extensions;

namespace pax.BlazorChess.Tests;

[TestClass]
public class AnalysisSerializerTests
{
    [TestMethod]
    public void Roundtrip_keeps_main_line_length()
    {
        var game = PgnSerializer.Parse("1. e4 e5 2. Nf3 Nc6");
        var board = new AnalysisBoard(game);

        var json = AnalysisSerializer.Serialize(board);
        var restored = AnalysisSerializer.Restore(json);

        Assert.AreEqual(GetMainLineCount(board.Root), GetMainLineCount(restored.Root));
        Assert.AreEqual(game.Moves.Count, restored.ChessGame.Moves.Count);
    }

    [TestMethod]
    public void Serialize_uses_flat_snapshot_format()
    {
        var board = new AnalysisBoard(PgnSerializer.Parse("1. e4 e5 2. Nf3 Nc6"));

        var json = AnalysisSerializer.Serialize(board);
        using var document = JsonDocument.Parse(json);

        Assert.IsTrue(document.RootElement.TryGetProperty("nodes", out var nodes));
        Assert.IsTrue(document.RootElement.TryGetProperty("rootId", out _));
        Assert.IsFalse(document.RootElement.TryGetProperty("root", out _));
        Assert.AreEqual(GetMainLineCount(board.Root) + 1, nodes.GetArrayLength());
    }

    [TestMethod]
    public void Roundtrip_long_main_line_exceeding_recursive_json_depth()
    {
        var board = CreateRepeatedKnightBoard(fullMoves: 140);

        var json = AnalysisSerializer.Serialize(board);
        var restored = AnalysisSerializer.Restore(json);

        Assert.AreEqual(280, GetMainLineCount(board.Root));
        Assert.AreEqual(GetMainLineCount(board.Root), GetMainLineCount(restored.Root));
        Assert.AreEqual(GetMainLineCount(board.Root), restored.ChessGame.Moves.Count);
    }

    [TestMethod]
    public void Roundtrip_keeps_variations_and_annotations()
    {
        var board = new AnalysisBoard(PgnSerializer.Parse("1. e4 e5"));
        board.MoveToNode(board.Root);
        var variation = Uci.CreateMove("d2d4", board.CurrentPosition)
            ?? throw new InvalidOperationException("Could not create variation move.");

        board.AddVariation(variation);
        board.CurrentNode.Note = "Queen pawn sideline";
        board.CurrentNode.Evaluation = 24;
        board.CurrentNode.Depth = 12;

        var restored = AnalysisSerializer.Restore(AnalysisSerializer.Serialize(board));
        var restoredVariation = restored.Root.Children.Single(n =>
            n.Move is not null && string.Equals(Uci.GetUci(n.Move), "d2d4", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(2, restored.Root.Children.Count);
        Assert.AreEqual("Queen pawn sideline", restoredVariation.Note);
        Assert.AreEqual(24, restoredVariation.Evaluation);
        Assert.AreEqual(12, restoredVariation.Depth);
    }

    [TestMethod]
    public void Restore_accepts_legacy_recursive_snapshot()
    {
        var snapshot = new AnalysisSnapshot(
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "1. e4 e5",
            new AnalysisNodeDto
            {
                Children =
                [
                    new AnalysisNodeDto
                    {
                        Uci = "e2e4",
                        San = "e4",
                        Children =
                        [
                            new AnalysisNodeDto
                            {
                                Uci = "e7e5",
                                San = "e5",
                                Note = "Legacy note",
                                Evaluation = -8,
                                Depth = 10
                            }
                        ]
                    }
                ]
            });
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var restored = AnalysisSerializer.Restore(json);
        var secondMove = restored.Root.MainLine?.MainLine;

        Assert.AreEqual(2, GetMainLineCount(restored.Root));
        Assert.AreEqual(2, restored.ChessGame.Moves.Count);
        Assert.AreEqual("Legacy note", secondMove?.Note);
        Assert.AreEqual(-8, secondMove?.Evaluation);
        Assert.AreEqual(10, secondMove?.Depth);
    }

    [TestMethod]
    public void Analysis_run_fingerprint_matches_same_move_list()
    {
        var game = PgnSerializer.Parse("1. e4 e5 2. Nf3 Nc6");
        var snapshot = new GameAnalysisRunSnapshot
        {
            MoveCount = game.Moves.Count,
            MoveListFingerprint = GameAnalysisRunFingerprint.Create(game)
        };

        Assert.IsTrue(GameAnalysisRunFingerprint.Matches(snapshot, game));
    }

    [TestMethod]
    public void Analysis_run_fingerprint_rejects_different_move_list()
    {
        var original = PgnSerializer.Parse("1. e4 e5 2. Nf3 Nc6");
        var changed = PgnSerializer.Parse("1. e4 c5 2. Nf3 Nc6");
        var snapshot = new GameAnalysisRunSnapshot
        {
            MoveCount = changed.Moves.Count,
            MoveListFingerprint = GameAnalysisRunFingerprint.Create(original)
        };

        Assert.IsFalse(GameAnalysisRunFingerprint.Matches(snapshot, changed));
    }

    [TestMethod]
    public void Analysis_run_fingerprint_allows_legacy_snapshot_by_move_count()
    {
        var game = PgnSerializer.Parse("1. d4 d5");
        var snapshot = new GameAnalysisRunSnapshot
        {
            MoveCount = game.Moves.Count
        };

        Assert.IsTrue(GameAnalysisRunFingerprint.Matches(snapshot, game));
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
}
