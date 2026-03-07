using pax.chess;
using System.Globalization;
using System.Text;

namespace pax.BlazorChess.Shared.Models;

public sealed record ActiveDrawing(int StartSquareIndex, string Color);
public sealed record BoardAnnotation(int FromSquareIndex, int ToSquareIndex, string Color, bool IsArrow)
{
    public static BoardAnnotation Arrow(int fromSquareIndex, int toSquareIndex, string color) =>
    new(fromSquareIndex, toSquareIndex, color, IsArrow: true);

    public static BoardAnnotation Circle(int squareIndex, string color) =>
        new(squareIndex, squareIndex, color, IsArrow: false);
}

public sealed class BoardAnnotationCollection
{
    private readonly List<BoardAnnotation> annotations = [];
    public IReadOnlyList<BoardAnnotation> Annotations => annotations;
    public void Add(BoardAnnotation annotation) => annotations.Add(annotation);
    public void Clear() => annotations.Clear();
    public ActiveDrawing? ActiveDrawing { get; set; }
    public int? ActiveHoverSquareIndex { get; set; }

}

public static class AnnotationSvgGenerator
{
    private static readonly string[] MarkerColors = ["#8bc34a", "#f44336", "#03a9f4", "#ff9800"];
    public static string GenerateSvg(BoardAnnotationCollection collection, bool blackAtBottom)
    {
        StringBuilder sb = new();
        sb.AppendLine("<svg class=\"board-overlay\" viewBox=\"0 0 100 100\" preserveAspectRatio=\"none\">");
        sb.AppendLine("<defs>");
        foreach (var markerColor in MarkerColors)
        {
            sb.AppendLine(@$"<marker id=""{GetMarkerId(markerColor)}""
                            markerWidth=""6""
                            markerHeight=""6""
                            refX=""5.2""
                            refY=""3""
                            orient=""auto""
                            markerUnits=""strokeWidth"" >
                        <path d=""M0,0 L0,6 L6,3 z"" fill=""@markerColor""></path>
                    </marker>");
        }
        sb.AppendLine("</defs>");

        foreach (var annotation in collection.Annotations)
        {
            var (x1, y1) = GetSquareCenter(annotation.FromSquareIndex, blackAtBottom);
            var (x2, y2) = GetSquareCenter(annotation.ToSquareIndex, blackAtBottom);
            if (annotation.IsArrow)
            {
                sb.AppendLine(@$"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" stroke=""{annotation.Color}"" stroke-width=""2"" marker-end=""url(#{GetMarkerId(annotation.Color)})""></line>");
            }
            else
            {
                sb.AppendLine(@$"<circle cx=""{x1}"" cy=""{y1}"" r=""5"" fill=""none"" stroke=""{annotation.Color}"" stroke-width=""2""></circle>");
            }
        }

        if (collection.ActiveDrawing is not null && collection.ActiveHoverSquareIndex is not null && collection.ActiveHoverSquareIndex != collection.ActiveDrawing.StartSquareIndex)
        {
            var (startX, startY) = GetSquareCenter(collection.ActiveDrawing.StartSquareIndex, blackAtBottom);
            var (endX, endY) = GetSquareCenter(collection.ActiveHoverSquareIndex.Value, blackAtBottom);

            sb.AppendLine(@$"<line x1=""{startX}"" y1=""{startY}"" x2=""{endX}"" y2=""{endY}"" class=""board-arrow board-arrow-preview"" stroke=""{collection.ActiveDrawing.Color}"" marker-end=""url(#{GetMarkerId(collection.ActiveDrawing.Color)})""></line>");
        }
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static string GetDoubleString(double d)
    {
        return d.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string GetMarkerId(string color)
    {
        return $"marker_{color.Replace("#", string.Empty, StringComparison.Ordinal)}";
    }

    private static (double x, double y) GetSquareCenter(int squareIndex, bool blackAtBottom)
    {
        var square = new Square(squareIndex);
        var viewFile = blackAtBottom ? 7 - square.File : square.File;
        var viewRank = blackAtBottom ? square.Rank : 7 - square.Rank;
        const double squareSize = 12.5;
        return ((viewFile + 0.5) * squareSize, (viewRank + 0.5) * squareSize);
    }
}