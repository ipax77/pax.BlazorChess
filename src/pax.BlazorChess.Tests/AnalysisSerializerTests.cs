using Microsoft.VisualStudio.TestTools.UnitTesting;
using pax.BlazorChess.Board.Storage;
using pax.chess;
using pax.chess.Analyze;

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
}
