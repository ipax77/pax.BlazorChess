using Microsoft.AspNetCore.Components;
using pax.chess;

namespace pax.BlazorChess.Shared;

public partial class ChessBoardComponent : ComponentBase
{
    private static readonly ChessGame DefaultGame = new();

    public Guid BoardGuid { get; } = Guid.NewGuid();

    [Parameter]
    public ChessGame? Game { get; set; }

    [Parameter]
    public bool WhiteAtBottom { get; set; } = true;

    [Parameter]
    public string BoardSize { get; set; } = "60vh";

    [Parameter]
    public bool Responsive { get; set; } = true;

    private string FinalBoardSize => Responsive ? "100%" : BoardSize;

    [Parameter]
    public string? ClassName { get; set; }

    [Parameter]
    public Square? SelectedSquare { get; set; }

    [Parameter]
    public Square? LastMoveFrom { get; set; }

    [Parameter]
    public Square? LastMoveTo { get; set; }

    [Parameter]
    public EventCallback<Square> OnSquareSelected { get; set; }

    private ChessGame CurrentGame => Game ?? DefaultGame;

    private bool IsLightSquare(int file, int rank)
    {
        return (file + rank) % 2 != 0;
    }

    private Task HandleSquareClick(Square square)
    {
        return OnSquareSelected.HasDelegate
            ? OnSquareSelected.InvokeAsync(square)
            : Task.CompletedTask;
    }

    private static string GetPieceAlt(Piece piece)
    {
        return $"{piece.Color} {piece.Type}";
    }

    private static string GetSquareLabel(Square square, Piece? piece)
    {
        return piece.HasValue
            ? $"{square}: {GetPieceAlt(piece.Value)}"
            : $"{square}: empty";
    }

    private static string GetPieceSvg(Piece piece)
    {
        var pieceChar = FenSerializer.GetPieceString(piece.Type).ToLowerInvariant();
        var colorChar = piece.Color == PieceColor.White ? "l" : "d";
        return $"_content/pax.BlazorChess.Shared/Images/pieces/Chess_{pieceChar}{colorChar}t45.svg";
    }

    private static string GetFileLabel(int file) => ((char)('a' + file)).ToString();
    private static string GetRankLabel(int rank) => (rank + 1).ToString();
}
