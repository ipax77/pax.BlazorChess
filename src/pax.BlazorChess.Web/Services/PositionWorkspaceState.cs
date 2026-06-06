using pax.chess;
using pax.chess.Analyze;

namespace pax.BlazorChess.Web.Services;

public sealed class PositionWorkspaceState
{
    private static readonly string StartFen = FenSerializer.Serialize(BoardPosition.CreateInitial());

    public event Action? Changed;

    public AnalysisBoard AnalysisBoard { get; private set; } = new(new ChessGame());
    public string Name { get; private set; } = "Start position";
    public string Source { get; private set; } = "Default";
    public string Fen => FenSerializer.Serialize(AnalysisBoard.CurrentPosition);

    public void SetFen(string fen, string source = "FEN")
    {
        var position = FenSerializer.Parse(string.IsNullOrWhiteSpace(fen) ? StartFen : fen.Trim());
        SetPosition(position, source);
    }

    public void SetPgn(string pgn, string source = "PGN")
    {
        var game = PgnSerializer.Parse(pgn);
        AnalysisBoard = new AnalysisBoard(game);
        Name = "Imported game";
        Source = source;
        Changed?.Invoke();
    }

    public void SetPosition(BoardPosition position, string source = "Manual")
    {
        ArgumentNullException.ThrowIfNull(position);

        AnalysisBoard = new AnalysisBoard(new ChessGame(position));
        Name = "Position";
        Source = source;
        Changed?.Invoke();
    }

    public void SetAnalysis(AnalysisBoard board, string name, string source = "History")
    {
        ArgumentNullException.ThrowIfNull(board);

        AnalysisBoard = board;
        Name = name;
        Source = source;
        Changed?.Invoke();
    }
}
