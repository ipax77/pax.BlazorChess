namespace pax.BlazorChess.Shared.Common;

[Flags]
public enum PlayColor
{
    White = 0,
    Black = 1 << 0,
}