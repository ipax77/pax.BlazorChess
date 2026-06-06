using pax.chess;

namespace pax.BlazorChess.Web.Models;

public sealed record PositionSnapshot(
    string Fen,
    PieceColor SideToMove,
    string CastlingRights,
    int MoveNumber,
    int HalfmoveClock,
    int PieceCount,
    IReadOnlyList<string> Warnings)
{
    public static PositionSnapshot FromPosition(BoardPosition position, IReadOnlyList<string>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(position);

        return new PositionSnapshot(
            FenSerializer.Serialize(position),
            position.SideToMove,
            FormatCastlingRights(position.CastlingRights),
            position.FullmoveNumber,
            position.HalfmoveClock,
            CountPieces(position),
            warnings ?? []);
    }

    private static int CountPieces(BoardPosition position)
    {
        var count = 0;
        for (var i = 0; i < 64; i++)
        {
            if (position.Board[i].HasValue)
                count++;
        }

        return count;
    }

    private static string FormatCastlingRights(pax.chess.CastlingRights rights)
    {
        if (rights == pax.chess.CastlingRights.None)
            return "-";

        Span<char> chars = stackalloc char[4];
        var index = 0;

        if (rights.HasFlag(pax.chess.CastlingRights.WhiteKingSide)) chars[index++] = 'K';
        if (rights.HasFlag(pax.chess.CastlingRights.WhiteQueenSide)) chars[index++] = 'Q';
        if (rights.HasFlag(pax.chess.CastlingRights.BlackKingSide)) chars[index++] = 'k';
        if (rights.HasFlag(pax.chess.CastlingRights.BlackQueenSide)) chars[index++] = 'q';

        return new string(chars[..index]);
    }
}
