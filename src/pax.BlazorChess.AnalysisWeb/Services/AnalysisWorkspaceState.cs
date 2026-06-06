using pax.chess;
using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.AnalysisWeb.Services;

public sealed class AnalysisWorkspaceState
{
    private const string DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public AnalysisBoard AnalysisBoard { get; private set; } = CreateInitialAnalysisBoard();
    public List<EngineRunOptions> EngineRunOptions { get; } = [];
    public Guid SelectedEngineId { get; set; }
    public Move? LastMove { get; private set; }
    public string ImportError { get; private set; } = string.Empty;

    public string Fen => FenSerializer.Serialize(AnalysisBoard.CurrentPosition);
    public string Pgn => BuildMainLinePgn();
    public EngineRunOptions? SelectedEngine => EngineRunOptions.FirstOrDefault(e => e.Id == SelectedEngineId);

    public void ApplyMove(Move move)
    {
        AnalysisBoard.AddVariation(move);
        LastMove = move;
        ImportError = string.Empty;
    }

    public void MoveBackward()
    {
        AnalysisBoard.MoveBackward();
        LastMove = AnalysisBoard.CurrentNode.Move;
    }

    public void MoveForward()
    {
        AnalysisBoard.MoveForward();
        LastMove = AnalysisBoard.CurrentNode.Move;
    }

    public void MoveToNode(MoveNode node)
    {
        AnalysisBoard.MoveToNode(node);
        LastMove = node.Move;
    }

    public void Reset()
    {
        AnalysisBoard = CreateInitialAnalysisBoard();
        LastMove = null;
        ImportError = string.Empty;
    }

    public bool TryLoadFen(string fen)
    {
        try
        {
            var position = FenSerializer.Parse(fen);
            AnalysisBoard = new AnalysisBoard(new ChessGame(position));
            LastMove = null;
            ImportError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            ImportError = $"Could not load FEN: {ex.Message}";
            return false;
        }
    }

    public bool TryLoadPgn(string pgn)
    {
        try
        {
            var game = PgnSerializer.Parse(pgn);
            AnalysisBoard = new AnalysisBoard(game);
            MoveToEnd();
            ImportError = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            ImportError = $"Could not load PGN: {ex.Message}";
            return false;
        }
    }

    public EngineRunOptions AddEngine()
    {
        var engine = new EngineRunOptions
        {
            Name = EngineRunOptions.Count == 0 ? "Stockfish" : $"Engine {EngineRunOptions.Count + 1}",
            Threads = Math.Max(1, Environment.ProcessorCount / 4),
            Pvs = 3,
            HashMb = 16,
            PoolSize = 1,
            IdelTimeoutMs = 5000
        };

        EngineRunOptions.Add(engine);
        SelectedEngineId = engine.Id;
        return engine;
    }

    public void DeleteEngine(EngineRunOptions engine)
    {
        EngineRunOptions.Remove(engine);
        if (SelectedEngineId == engine.Id)
            SelectedEngineId = EngineRunOptions.FirstOrDefault()?.Id ?? Guid.Empty;
    }

    private void MoveToEnd()
    {
        var current = AnalysisBoard.Root;
        while (current.MainLine is not null)
            current = current.MainLine;

        AnalysisBoard.MoveToNode(current);
        LastMove = current.Move;
    }

    private string BuildMainLinePgn()
    {
        List<string> parts = [];
        var current = AnalysisBoard.Root.MainLine;
        var ply = 0;
        while (current is not null)
        {
            if (ply % 2 == 0)
                parts.Add($"{(ply / 2) + 1}.");

            parts.Add(current.San);
            current = current.MainLine;
            ply++;
        }

        return parts.Count == 0 ? string.Empty : string.Join(' ', parts);
    }

    private static AnalysisBoard CreateInitialAnalysisBoard()
        => new(new ChessGame(FenSerializer.Parse(DefaultFen)));
}
