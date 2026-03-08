using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using pax.BlazorChess.Shared.Models;
using pax.chess;
using pax.chess.Validation;

namespace pax.BlazorChess.Shared.Components;

public partial class BoardPositionComponent
{
    [Parameter, EditorRequired]
    public required BoardPosition Position { get; set; }

    [Parameter]
    public bool BlackAtBottom { get; set; }

    [Parameter]
    public ChessGame? Game { get; set; }

    Guid BoardGuid { get; } = Guid.NewGuid();

    private IJSObjectReference? module;

    private List<Square> validDestinations = [];

    private Square? selectedSquare;
    private Square? pointerDownSquare;

    private bool pointerDown;
    private bool dragging;

    private int? activePointerId;

    private double startX;
    private double startY;

    private double dragX;
    private double dragY;

    private string? dragPieceSvg;

    private const double DragThreshold = 6;

    private Move? lastMove => Game?.Moves.LastOrDefault()?.Move;
    private readonly BoardAnnotationCollection boardAnnotation = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/pax.BlazorChess.Shared/Components/BoardPlayComponent.razor.js");
            await module.InvokeVoidAsync("updateBoardSize", BoardGuid);
        }
    }

    private void ApplyMove(Square from, Square to)
    {
        if (Game is null) return;

        var move = new Move(from, to);

        var piece = Game.CurrentPosition.Board[from.Index];
        if (piece?.Type == PieceType.Pawn && (to.Rank == 0 || to.Rank == 7))
        {
            move = new Move(from, to, PieceType.Queen);
        }
        else
        {
            var kingSquare = Game.CurrentPosition.Board.GetKingSquare(Game.CurrentPosition.SideToMove);
            if (from == kingSquare)
            {
                // Handle castling moves
                if (to.File == 6) // kingside
                    move = new Move(from, to, null, MoveType.CastlingKingSide);
                else if (to.File == 2) // queenside
                    move = new Move(from, to, null, MoveType.CastlingQueenSide);
            }
        }

        var state = Game.TryApplyMove(move);

        if (state == MoveState.Ok)
        {
            Position = Game.CurrentPosition;
            boardAnnotation.Clear();
            StateHasChanged();
        }
    }

    private async Task OnPointerDown(Square square, PointerEventArgs e)
    {
        if (e.Button == 2)
        {
            boardAnnotation.OnPointerDown(square.Index, e);
            StateHasChanged();
        }

        if (e.Button != 0)
            return;

        pointerDown = true;
        activePointerId = (int)e.PointerId;
        pointerDownSquare = square;

        startX = e.ClientX;
        startY = e.ClientY;

        dragX = startX;
        dragY = startY;

        var piece = Game?.CurrentPosition.Board[square.Index];

        if (piece.HasValue)
            dragPieceSvg = GetPieceSvg(piece.Value);

        if (module is not null)
            await module.InvokeVoidAsync("setPointerCapture", BoardGuid, activePointerId.Value);
    }

    private async Task OnPointerMove(PointerEventArgs e)
    {
        var index = await GetAnnotationSquareIndexFromPoint(e);
        if (index.HasValue)
        {
            if (boardAnnotation.OnPointerMove(index.Value, e))
                StateHasChanged();
            return;
        }

        if (!pointerDown || activePointerId != (int)e.PointerId)
            return;

        dragX = e.ClientX;
        dragY = e.ClientY;

        if (!dragging && pointerDownSquare.HasValue)
        {
            var dx = dragX - startX;
            var dy = dragY - startY;

            if ((dx * dx + dy * dy) > DragThreshold * DragThreshold)
            {
                if (IsValidPieceSelection(pointerDownSquare.Value))
                {
                    dragging = true;
                    selectedSquare = pointerDownSquare;
                    validDestinations = GetValidMoveDestinations(pointerDownSquare.Value);
                }
            }
        }

        if (dragging)
            StateHasChanged();
    }

    private async Task OnPointerUp(PointerEventArgs e)
    {
        var index = await GetAnnotationSquareIndexFromPoint(e);
        if (index.HasValue)
        {
            boardAnnotation.OnSquareMouseUp(index.Value, e);
            StateHasChanged();
        }

        if (!pointerDown || activePointerId != (int)e.PointerId)
            return;

        if (module is not null)
            await module.InvokeVoidAsync("releasePointerCapture", BoardGuid, activePointerId.Value);

        if (dragging)
        {
            await HandleDragDrop(e);
        }
        else
        {
            HandleClick(pointerDownSquare);
        }

        ResetPointerState();
        StateHasChanged();
    }

    private async Task OnPointerCancel(PointerEventArgs e)
    {
        if (activePointerId != (int)e.PointerId)
            return;

        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("releasePointerCapture", BoardGuid, activePointerId.Value);
            }
            catch
            {
                // safe to ignore if already released
            }
        }

        ResetPointerState();
        StateHasChanged();
    }

    private async Task HandleDragDrop(PointerEventArgs e)
    {
        if (module is null || pointerDownSquare is null)
            return;

        var index = await module.InvokeAsync<int?>(
            "getSquareIndexFromPoint",
            e.ClientX,
            e.ClientY,
            BoardGuid);

        if (!index.HasValue)
            return;

        var dropSquare = new Square(index.Value % 8, index.Value / 8);

        if (validDestinations.Contains(dropSquare))
            ApplyMove(pointerDownSquare.Value, dropSquare);
    }

    private void HandleClick(Square? square)
    {
        if (!square.HasValue)
            return;

        if (selectedSquare == null)
        {
            if (IsValidPieceSelection(square.Value))
            {
                selectedSquare = square;
                validDestinations = GetValidMoveDestinations(square.Value);
            }
        }
        else
        {
            if (square == selectedSquare)
            {
                selectedSquare = null;
                validDestinations.Clear();
                StateHasChanged();
                return;
            }

            if (validDestinations.Contains(square.Value))
                ApplyMove(selectedSquare.Value, square.Value);

            selectedSquare = null;
            validDestinations.Clear();
        }

        StateHasChanged();
    }

    private void ResetPointerState()
    {
        pointerDown = false;
        dragging = false;

        activePointerId = null;
        pointerDownSquare = null;

        dragPieceSvg = null;
    }

    private async Task<int?> GetAnnotationSquareIndexFromPoint(PointerEventArgs e)
    {
        if (boardAnnotation.ActiveDrawing is null || boardAnnotation.DrawingPointerId != (int)e.PointerId)
            return null;

        if (module is null)
            return null;
        try
        {
            return await module.InvokeAsync<int?>(
                "getSquareIndexFromPoint",
                e.ClientX,
                e.ClientY,
                BoardGuid);
        }
        catch (JSException)
        {
            return null;
        }
    }

    private string GetDragPreviewStyle()
    {
        return $"left:{dragX}px;top:{dragY}px;transform:translate(-50%, -50%);";
    }

    private bool IsValidPieceSelection(Square square)
    {
        var piece = Game?.CurrentPosition.Board[square.Index];

        return piece.HasValue
            // && PlayColor == piece.Value.Color // DEBUG: allow moving opponent's pieces for testing purposes
            && piece.Value.Color == Position.SideToMove;
    }

    private List<Square> GetValidMoveDestinations(Square from)
    {
        MoveState moveState;
        var moves = MoveValidator.GetValidMoves(from, Position, out moveState);
        return moves.Select(m => m.To).ToList();
    }

    private string GetSquareCssClass(Square square)
    {
        var classes = new List<string>();

        classes.Add(IsLightSquare(square.File, square.Rank) ? "square light" : "square dark");

        if (lastMove != null && (lastMove.From == square || lastMove.To == square))
            classes.Add("last-move");

        if (pointerDownSquare.HasValue && pointerDownSquare.Value == square)
            classes.Add("selected");

        if (validDestinations.Contains(square))
            classes.Add("valid-destination");

        return string.Join(' ', classes);
    }

    private static bool IsLightSquare(int file, int rank)
    {
        return (file + rank) % 2 != 0;
    }

    private static string GetSquareLabel(Square square, Piece? piece)
    {
        return piece.HasValue
            ? $"{square}: {GetPieceAlt(piece.Value)}"
            : $"{square}: empty";
    }

    private static string GetPieceSvg(Piece piece)
    {
        var pieceChar = FenSerializer.GetPieceString(piece.Type);
        var colorChar = piece.Color == PieceColor.White ? "l" : "d";
        return $"_content/pax.BlazorChess.Shared/Images/pieces/Chess_{pieceChar}{colorChar}t45.svg";
    }

    private static string GetPieceAlt(Piece piece)
    {
        return $"{piece.Color} {piece.Type}";
    }

    private static string GetFileLabel(int file) => ((char)('a' + file)).ToString();
    private static string GetRankLabel(int rank) => (rank + 1).ToString();

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}