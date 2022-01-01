using pax.chess;

namespace pax.BlazorChess.Models;
public record Annotation
{
    public int X { get; init; }
    public int Y { get; init; }
    public string Color { get; init; }
    public double Length { get; init; }
    public double Angle { get; init; }
    

    public Annotation(Position start, string color, Position? end = null)
    {
        X = start.X;
        Y = start.Y;
        Color = color;
        Length = end == null ? 0 : Math.Sqrt(Math.Pow(start.X - end.X, 2) + Math.Pow(start.Y - end.Y, 2));
        Angle = end == null ? 0 : (180.0 / Math.PI) * Math.Atan2(start.Y - end.Y, end.X - start.X);
    }
}
