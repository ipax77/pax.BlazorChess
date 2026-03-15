using System.Text.Json;
using System.Text.Json.Serialization;
using pax.chess;
using pax.chess.Analyze;
using pax.chess.Extensions;

namespace pax.BlazorChess.Board.Storage;

public static class AnalysisSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        MaxDepth = 256,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static AnalysisSnapshot CreateSnapshot(AnalysisBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var initialFen = FenSerializer.Serialize(board.ChessGame.InitialPosition);
        var pgn = PgnSerializer.Serialize(board.ChessGame);
        var root = ToDto(board.Root);

        return new AnalysisSnapshot(initialFen, pgn, root);
    }

    public static string Serialize(AnalysisBoard board)
    {
        var snapshot = CreateSnapshot(board);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static AnalysisBoard Restore(string json)
    {
        var snapshot = JsonSerializer.Deserialize<AnalysisSnapshot>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize analysis snapshot.");

        return Restore(snapshot);
    }

    public static AnalysisBoard Restore(AnalysisSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var initialPosition = FenSerializer.Parse(snapshot.InitialFen ?? string.Empty);
        var game = BuildGameFromSnapshot(snapshot, initialPosition);

        var board = new AnalysisBoard(game);
        MergeTree(board, board.Root, snapshot.Root);
        board.MoveToNode(board.Root);

        return board;
    }

    private static ChessGame BuildGameFromSnapshot(AnalysisSnapshot snapshot, BoardPosition initialPosition)
    {
        var game = new ChessGame(initialPosition, new GameMetadata());
        var currentPosition = initialPosition;

        foreach (var move in EnumerateMainLineMoves(snapshot.Root, initialPosition))
        {
            var san = PgnSerializer.ToSan(move, currentPosition);
            game.ApplyMove(move, san);
            currentPosition = currentPosition.MakeMove(move);
        }

        return game;
    }

    private static IEnumerable<Move> EnumerateMainLineMoves(AnalysisNodeDto root, BoardPosition start)
    {
        var current = root.Children.FirstOrDefault();
        var position = start;

        while (current is not null)
        {
            var move = CreateMove(current, position);
            yield return move;
            position = position.MakeMove(move);
            current = current.Children.FirstOrDefault();
        }
    }

    private static AnalysisNodeDto ToDto(MoveNode node)
    {
        return new AnalysisNodeDto
        {
            Uci = node.Move is null ? null : Uci.GetUci(node.Move),
            San = node.San,
            Note = node.Note,
            Evaluation = node.Evaluation,
            Depth = node.Depth,
            Children = node.Children.Select(ToDto).ToList()
        };
    }

    private static void MergeTree(AnalysisBoard board, MoveNode actualNode, AnalysisNodeDto snapshotNode)
    {
        ApplyAnnotations(actualNode, snapshotNode);

        foreach (var childDto in snapshotNode.Children)
        {
            var matchingChild = FindChild(board, actualNode, childDto);
            MergeTree(board, matchingChild, childDto);
        }
    }

    private static MoveNode FindChild(AnalysisBoard board, MoveNode parent, AnalysisNodeDto childDto)
    {
        if (string.IsNullOrWhiteSpace(childDto.Uci))
            throw new InvalidOperationException("Snapshot node is missing UCI move notation.");

        var existing = parent.Children.FirstOrDefault(c =>
            c.Move is not null &&
            string.Equals(Uci.GetUci(c.Move), childDto.Uci, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            ApplyAnnotations(existing, childDto);
            return existing;
        }

        board.MoveToNode(parent);
        var move = CreateMove(childDto, board.CurrentPosition);
        board.AddVariation(move);
        var created = board.CurrentNode;

        ApplyAnnotations(created, childDto);

        return created;
    }

    private static Move CreateMove(AnalysisNodeDto node, BoardPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);

        var move = Uci.CreateMove(node.Uci ?? string.Empty, position)
            ?? throw new InvalidOperationException($"Invalid UCI move '{node.Uci}'.");

        return move;
    }

    private static void ApplyAnnotations(MoveNode node, AnalysisNodeDto snapshot)
    {
        node.Note = snapshot.Note;
        node.Evaluation = snapshot.Evaluation;
        node.Depth = snapshot.Depth;
    }
}

public sealed record AnalysisSnapshot(string InitialFen, string? Pgn, AnalysisNodeDto Root);

public sealed class AnalysisNodeDto
{
    public string? Uci { get; set; }
    public string? San { get; set; }
    public string? Note { get; set; }
    public int? Evaluation { get; set; }
    public int? Depth { get; set; }
    public List<AnalysisNodeDto> Children { get; set; } = [];
}
