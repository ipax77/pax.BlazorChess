using pax.chess;
using pax.uciChessEngine;

namespace pax.BlazorChess.Board;

public sealed class EngineGameSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public EngineGame EngineGame { get; init; } = default!;
    public ChessGame Game { get; init; } = default!;
    public ChessClock Clock { get; init; } = default!;
    public ClockComponent? ClockComponent { get; set; }
    public BoardComponent? BoardComponent { get; set; }
    public bool DisplayAttached { get; set; }
    public bool IsRunning { get; set; }
    public string WhiteEngine { get; init; } = string.Empty;
    public string BlackEngine { get; init; } = string.Empty;
    public string? Result { get; set; }
}