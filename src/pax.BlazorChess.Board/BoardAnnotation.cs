
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using pax.chess;

namespace pax.BlazorChess.Board;

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
    public static readonly string[] MarkerColors = ["#8bc34a", "#f44336", "#03a9f4", "#ff9800"];
    private readonly List<BoardAnnotation> annotations = [];
    public int? DrawingPointerId { get; private set; }

    public IReadOnlyList<BoardAnnotation> Annotations => annotations;
    public void Add(BoardAnnotation annotation) => annotations.Add(annotation);
    public void Clear() => annotations.Clear();
    public ActiveDrawing? ActiveDrawing { get; set; }
    public int? ActiveHoverSquareIndex { get; set; }

    public void ResetDrawingState()
    {
        ActiveDrawing = null;
        ActiveHoverSquareIndex = null;
        DrawingPointerId = null;
    }

    public void OnPointerDown(int squareIndex, PointerEventArgs e)
    {
        if (e.Button != 2)
            return;
        ActiveDrawing = new ActiveDrawing(squareIndex, SelectDrawColor(e));
        ActiveHoverSquareIndex = squareIndex;
        DrawingPointerId = (int)e.PointerId;
    }

    public bool OnPointerMove(int squareIndex, PointerEventArgs e)
    {
        if (ActiveDrawing is null || (int)e.PointerId != DrawingPointerId)
            return false;

        if ((e.Buttons & 2) != 2)
            return false;

        if (ActiveHoverSquareIndex == squareIndex)
            return false;
        
        ActiveHoverSquareIndex = squareIndex;
        return true;
    }

    public void OnSquareMouseEnter(int squareIndex)
    {
        ActiveHoverSquareIndex = squareIndex;
    }

    public void OnSquareMouseUp(int squareIndex, PointerEventArgs e)
    {
        if (ActiveDrawing is null || e.Button != 2)
            return;

        var drawing = ActiveDrawing;
        if (drawing.StartSquareIndex == squareIndex)
        {
            ToggleCircle(squareIndex, drawing.Color);
        }
        else
        {
            ToggleArrow(drawing.StartSquareIndex, squareIndex, drawing.Color);
        }
        ActiveDrawing = null;
        ActiveHoverSquareIndex = null;
    }

    private void ToggleCircle(int squareIndex, string color)
    {
        var existingIndex = annotations.FindIndex(a =>
            !a.IsArrow &&
            a.FromSquareIndex == squareIndex &&
            string.Equals(a.Color, color, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            annotations.RemoveAt(existingIndex);
            return;
        }

        annotations.Add(BoardAnnotation.Circle(squareIndex, color));
    }

    private void ToggleArrow(int startSquareIndex, int endSquareIndex, string color)
    {
        var existingIndex = annotations.FindIndex(a =>
            a.IsArrow &&
            a.FromSquareIndex == startSquareIndex &&
            a.ToSquareIndex == endSquareIndex &&
            string.Equals(a.Color, color, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            annotations.RemoveAt(existingIndex);
            return;
        }

        annotations.Add(BoardAnnotation.Arrow(startSquareIndex, endSquareIndex, color));
    }

    private static string SelectDrawColor(PointerEventArgs args)
    {
        if (args.ShiftKey && args.CtrlKey)
            return MarkerColors[3];

        if (args.ShiftKey)
            return MarkerColors[1];

        if (args.CtrlKey)
            return MarkerColors[2];

        return MarkerColors[0];
    }
}

public static class BoardAnnotationCollectionExtensions
{
    public static MarkupString GenerateSvg(this BoardAnnotationCollection collection, bool blackAtBottom)
    {
        StringBuilder sb = new();
        sb.AppendLine("<svg class=\"board-overlay\" viewBox=\"0 0 100 100\" preserveAspectRatio=\"none\">");
        sb.AppendLine("<defs>");
        foreach (var markerColor in BoardAnnotationCollection.MarkerColors)
        {
            sb.AppendLine(@$"<marker id=""{GetMarkerId(markerColor)}""
                            markerWidth=""6""
                            markerHeight=""6""
                            refX=""5.2""
                            refY=""3""
                            orient=""auto""
                            markerUnits=""strokeWidth"" >
                        <path d=""M0,0 L0,6 L6,3 z"" fill=""{markerColor}""></path>
                    </marker>");
        }
        sb.AppendLine("</defs>");

        foreach (var annotation in collection.Annotations)
        {
            var (x1, y1) = GetSquareCenter(annotation.FromSquareIndex, blackAtBottom);
            var (x2, y2) = GetSquareCenter(annotation.ToSquareIndex, blackAtBottom);
            if (annotation.IsArrow)
            {
                sb.AppendLine(@$"<line x1=""{GetDoubleString(x1)}"" y1=""{GetDoubleString(y1)}"" x2=""{GetDoubleString(x2)}"" y2=""{GetDoubleString(y2)}"" class=""board-arrow board-arrow-preview"" stroke=""{annotation.Color}"" marker-end=""url(#{GetMarkerId(annotation.Color)})""></line>");
            }
            else
            {
                sb.AppendLine(@$"<circle cx=""{GetDoubleString(x1)}"" cy=""{GetDoubleString(y1)}"" r=""5"" fill=""none"" stroke=""{annotation.Color}"" stroke-width=""2""></circle>");
            }
        }

        if (collection.ActiveDrawing is not null && collection.ActiveHoverSquareIndex is not null && collection.ActiveHoverSquareIndex != collection.ActiveDrawing.StartSquareIndex)
        {
            var (startX, startY) = GetSquareCenter(collection.ActiveDrawing.StartSquareIndex, blackAtBottom);
            var (endX, endY) = GetSquareCenter(collection.ActiveHoverSquareIndex.Value, blackAtBottom);

            sb.AppendLine(@$"<line x1=""{GetDoubleString(startX)}"" y1=""{GetDoubleString(startY)}"" x2=""{GetDoubleString(endX)}"" y2=""{GetDoubleString(endY)}"" class=""board-arrow board-arrow-preview"" stroke=""{collection.ActiveDrawing.Color}"" marker-end=""url(#{GetMarkerId(collection.ActiveDrawing.Color)})""></line>");
        }
        sb.AppendLine("</svg>");

        return new MarkupString(sb.ToString());
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