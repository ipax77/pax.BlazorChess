using System.Text.Json;
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
        MaxDepth = 256
    };

    private static readonly JsonDocumentOptions JsonDocumentOptions = new()
    {
        MaxDepth = 256
    };

    public static FlatAnalysisSnapshot CreateSnapshot(AnalysisBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var initialFen = FenSerializer.Serialize(board.ChessGame.InitialPosition);
        var pgn = PgnSerializer.Serialize(board.ChessGame);
        var rootId = Guid.NewGuid();
        var nodes = ToFlatNodes(board.Root, rootId);

        return new FlatAnalysisSnapshot(initialFen, pgn, rootId, nodes);
    }

    public static string Serialize(AnalysisBoard board)
    {
        var snapshot = CreateSnapshot(board);
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static AnalysisBoard Restore(string json)
    {
        using var document = JsonDocument.Parse(json, JsonDocumentOptions);
        var root = document.RootElement;
        if (root.TryGetProperty("nodes", out _))
        {
            var flatSnapshot = root.Deserialize<FlatAnalysisSnapshot>(JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize analysis snapshot.");

            return Restore(flatSnapshot);
        }

        var legacySnapshot = root.Deserialize<AnalysisSnapshot>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize legacy analysis snapshot.");

        return Restore(legacySnapshot);
    }

    public static AnalysisBoard Restore(FlatAnalysisSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var initialPosition = FenSerializer.Parse(snapshot.InitialFen ?? string.Empty);
        var root = BuildLegacyTree(snapshot);
        var board = new AnalysisBoard(new ChessGame(initialPosition));
        MergeTree(board, board.Root, root);
        board.MoveToNode(board.Root);

        return board;
    }

    public static AnalysisBoard Restore(AnalysisSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var initialPosition = FenSerializer.Parse(snapshot.InitialFen ?? string.Empty);
        var board = new AnalysisBoard(new ChessGame(initialPosition));
        MergeTree(board, board.Root, snapshot.Root);
        board.MoveToNode(board.Root);

        return board;
    }

    private static List<FlatAnalysisNodeDto> ToFlatNodes(MoveNode root, Guid rootId)
    {
        var nodes = new List<FlatAnalysisNodeDto>(CountNodes(root));
        var nodeIds = new Dictionary<MoveNode, Guid>(ReferenceEqualityComparer.Instance)
        {
            [root] = rootId
        };
        var visited = new HashSet<MoveNode>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<(MoveNode Node, Guid Id, Guid? ParentId, int Order)>();
        stack.Push((root, rootId, null, 0));

        while (stack.Count > 0)
        {
            var (node, nodeId, parentId, order) = stack.Pop();
            if (!visited.Add(node))
                throw new InvalidOperationException("Analysis tree contains a cycle or shared node reference.");

            nodes.Add(new FlatAnalysisNodeDto
            {
                Id = nodeId,
                ParentId = parentId,
                Order = order,
                Uci = node.Move is null ? null : Uci.GetUci(node.Move),
                San = node.San,
                Note = node.Note,
                Evaluation = node.Evaluation,
                Depth = node.Depth
            });

            for (var childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
            {
                var child = node.Children[childIndex];
                var childId = Guid.NewGuid();
                if (!nodeIds.TryAdd(child, childId))
                    throw new InvalidOperationException("Analysis tree contains a cycle or shared node reference.");

                stack.Push((child, childId, nodeId, childIndex));
            }
        }

        return nodes;
    }

    private static int CountNodes(MoveNode root)
    {
        var count = 0;
        var visited = new HashSet<MoveNode>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<MoveNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node))
                throw new InvalidOperationException("Analysis tree contains a cycle or shared node reference.");

            count++;
            for (var i = 0; i < node.Children.Count; i++)
                stack.Push(node.Children[i]);
        }

        return count;
    }

    private static AnalysisNodeDto BuildLegacyTree(FlatAnalysisSnapshot snapshot)
    {
        if (snapshot.Nodes.Count == 0)
            throw new InvalidOperationException("Analysis snapshot does not contain any nodes.");

        var byId = new Dictionary<Guid, AnalysisNodeDto>(snapshot.Nodes.Count);
        foreach (var flatNode in snapshot.Nodes)
        {
            if (!byId.TryAdd(flatNode.Id, new AnalysisNodeDto
                {
                    Uci = flatNode.Uci,
                    San = flatNode.San,
                    Note = flatNode.Note,
                    Evaluation = flatNode.Evaluation,
                    Depth = flatNode.Depth
                }))
            {
                throw new InvalidOperationException($"Analysis snapshot contains duplicate node id '{flatNode.Id}'.");
            }
        }

        var orderedNodes = snapshot.Nodes.OrderBy(n => n.Order).ToList();
        foreach (var flatNode in orderedNodes)
        {
            if (flatNode.ParentId is null)
                continue;

            if (!byId.TryGetValue(flatNode.ParentId.Value, out var parent))
                throw new InvalidOperationException($"Analysis snapshot node '{flatNode.Id}' references missing parent '{flatNode.ParentId}'.");

            parent.Children.Add(byId[flatNode.Id]);
        }

        var rootNodes = snapshot.Nodes.Where(n => n.ParentId is null).ToList();
        if (rootNodes.Count != 1 || rootNodes[0].Id != snapshot.RootId)
            throw new InvalidOperationException("Analysis snapshot must contain exactly one root node.");

        return byId[snapshot.RootId];
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

public sealed record FlatAnalysisSnapshot(
    string InitialFen,
    string? Pgn,
    Guid RootId,
    IReadOnlyList<FlatAnalysisNodeDto> Nodes);

public sealed class FlatAnalysisNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; }
    public string? Uci { get; set; }
    public string? San { get; set; }
    public string? Note { get; set; }
    public int? Evaluation { get; set; }
    public int? Depth { get; set; }
}

public sealed class AnalysisNodeDto
{
    public string? Uci { get; set; }
    public string? San { get; set; }
    public string? Note { get; set; }
    public int? Evaluation { get; set; }
    public int? Depth { get; set; }
    public List<AnalysisNodeDto> Children { get; set; } = [];
}
