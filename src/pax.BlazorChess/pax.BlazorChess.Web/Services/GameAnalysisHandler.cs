using pax.BlazorChess.Shared.Interfaces;
using pax.chess;
using pax.uciChessEngine;

namespace pax.BlazorChess.Web.Services;

public class GameAnalysisHandler : IGameAnalysisHandler
{
    private readonly string engineBinary = @"C:\data\chess\engines\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe";

    public GameAnalysis Create(ChessGame game, int threads = 8)
    {
        return new GameAnalysis(engineBinary, game, threads);
    }
}
