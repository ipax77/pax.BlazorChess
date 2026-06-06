using Microsoft.AspNetCore.Components.Web;
using pax.chess.Analyze;

namespace pax.BlazorChess.Board;

public enum MoveContextAction
{
    DeleteFromHere,
    MakeMainVariation
}

public sealed record MoveContextMenuRequest(MoveNode Node, MouseEventArgs Args);

public sealed record MoveContextActionRequest(MoveNode Node, MoveContextAction Action);
