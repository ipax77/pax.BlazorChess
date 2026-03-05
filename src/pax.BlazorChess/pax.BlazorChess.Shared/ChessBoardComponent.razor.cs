using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using pax.chess;

namespace pax.BlazorChess.Shared;

public partial class ChessBoardComponent : ComponentBase, IAsyncDisposable
{
    private static readonly ChessGame DefaultGame = new();

    public string BoardGuid { get; } = $"board_{Guid.NewGuid():N}";

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private IJSObjectReference? _module;

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

    private Square? _draggingSquare;
    private double _dragX;
    private double _dragY;
    private double _startX;
    private double _startY;
    private long _pointerId;
    private bool _isMoveTriggered;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/pax.BlazorChess.Shared/ChessBoardComponent.razor.js");
        }
    }

    private bool IsLightSquare(int file, int rank)
    {
        return (file + rank) % 2 != 0;
    }

    private async Task HandleSquareClick(Square square)
    {
        if (_isMoveTriggered)
        {
            _isMoveTriggered = false;
            return;
        }

        await ProcessClick(square);
    }

    private async Task ProcessClick(Square square)
    {
        var piece = CurrentGame.CurrentPosition.Board[square.Index];
        var isOwnPiece = piece.HasValue && piece.Value.Color == CurrentGame.CurrentPosition.SideToMove;

        if (SelectedSquare == null)
        {
            if (isOwnPiece)
            {
                SelectedSquare = square;
            }
        }
        else
        {
            if (SelectedSquare.Value.Index == square.Index)
            {
                SelectedSquare = null;
            }
            else if (isOwnPiece)
            {
                // Switch selection to another of our pieces
                SelectedSquare = square;
            }
            else
            {
                // Try to move to this square (enemy piece or empty)
                Move(SelectedSquare.Value, square);
                SelectedSquare = null;
            }
        }

        if (OnSquareSelected.HasDelegate)
        {
            await OnSquareSelected.InvokeAsync(square);
        }
    }

    private async Task HandlePointerDown(PointerEventArgs e, Square square)
    {
        var piece = CurrentGame.CurrentPosition.Board[square.Index];
        if (piece.HasValue && piece.Value.Color == CurrentGame.CurrentPosition.SideToMove)
        {
            _draggingSquare = square;
            _startX = e.ClientX;
            _startY = e.ClientY;
            _dragX = 0;
            _dragY = 0;
            _pointerId = e.PointerId;
            _isMoveTriggered = false;

            if (_module != null)
            {
                await _module.InvokeVoidAsync("setPointerCapture", BoardGuid, e.PointerId);
            }
        }
    }

    private void HandlePointerMove(PointerEventArgs e)
    {
        if (_draggingSquare.HasValue && e.PointerId == _pointerId)
        {
            _dragX = e.ClientX - _startX;
            _dragY = e.ClientY - _startY;
            
            // If we move more than 5 pixels, consider it a drag/move intent
            if (!_isMoveTriggered && (Math.Abs(_dragX) > 5 || Math.Abs(_dragY) > 5))
            {
                _isMoveTriggered = true;
            }
        }
    }

    private async Task HandlePointerUp(PointerEventArgs e, Square? square)
    {
        if (_draggingSquare.HasValue && e.PointerId == _pointerId)
        {
            Square? targetSquare = square;

            if (_module != null)
            {
                var targetId = await _module.InvokeAsync<string?>("getElementIdAtPoint", e.ClientX, e.ClientY);
                if (!string.IsNullOrEmpty(targetId) && targetId.StartsWith($"{BoardGuid}_"))
                {
                    if (int.TryParse(targetId.Substring(BoardGuid.Length + 1), out var index))
                    {
                        targetSquare = new Square(index);
                    }
                }
                
                await _module.InvokeVoidAsync("releasePointerCapture", BoardGuid, _pointerId);
            }

            if (targetSquare.HasValue)
            {
                if (_draggingSquare.Value.Index != targetSquare.Value.Index)
                {
                    // It was a drag and drop to a different square
                    Move(_draggingSquare.Value, targetSquare.Value);
                    SelectedSquare = null;
                    _isMoveTriggered = true; // Block the next 'click' event
                }
                else
                {
                    // It was a click on the piece (no significant drag)
                    if (!_isMoveTriggered)
                    {
                        await ProcessClick(targetSquare.Value);
                        _isMoveTriggered = true; // Block the next 'click' event
                    }
                }
            }
        }

        _draggingSquare = null;
        _dragX = 0;
        _dragY = 0;
    }

    private void Move(Square from, Square to)
    {
        var move = new Move(from, to);
        var moveResult = CurrentGame.TryApplyMove(move);
        if (moveResult == MoveState.Ok)
        {
            LastMoveFrom = from;
            LastMoveTo = to;
            StateHasChanged();
        }
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

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
}
