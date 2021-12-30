namespace pax.BlazorChess.Models;
public record ClickPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public ClickPosition(double x, double y)
    {
        X = x;
        Y = y;
    }
}