using pax.chess;

namespace pax.BlazorChess.Services;
public static class GuiService
{
    public static string GetImage(Piece piece) =>
        (piece.Type, piece.IsBlack) switch
        {
            (PieceType.Pawn, false) => "images/alpha/WhitePawn.png",
            (PieceType.Knight, false) => "images/alpha/WhiteKnight.png",
            (PieceType.Bishop, false) => "images/alpha/WhiteBishop.png",
            (PieceType.Rook, false) => "images/alpha/WhiteRook.png",
            (PieceType.Queen, false) => "images/alpha/WhiteQueen.png",
            (PieceType.King, false) => "images/alpha/WhiteKing.png",
            (PieceType.Pawn, true) => "images/alpha/BlackPawn.png",
            (PieceType.Knight, true) => "images/alpha/BlackKnight.png",
            (PieceType.Bishop, true) => "images/alpha/BlackBishop.png",
            (PieceType.Rook, true) => "images/alpha/BlackRook.png",
            (PieceType.Queen, true) => "images/alpha/BlackQueen.png",
            (PieceType.King, true) => "images/alpha/BlackKing.png",
            _ => throw new NotImplementedException()
        };

    public static string GetHintColor(int i) => i switch
    {
        0 => "#862d59",
        1 => "#ac3973",
        2 => "#c6538c",
        3 => "#d279a6",
        4 => "#df9fbf",
        5 => "#ecc6d9",
        _ => "#f9ecf2"
    };
}
