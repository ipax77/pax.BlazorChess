using pax.BlazorChess.Shared.Interfaces;
using pax.chess;
using pax.uciChessEngine;

namespace pax.BlazorChess.Web.Services;

public class EngineGameHandler : IEngineGameHandler
{
    private readonly string engineBinary = @"C:\data\chess\engines\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe";
    private readonly string engineBinary2 = @"C:\data\chess\engines\lc0-v0.32.1-windows-gpu-nvidia-cuda11\lc0.exe";
    public IEngineGame GetEngineGame(ChessGame game)
    {
        var engine1 = new UciEngine(engineBinary2);
        var engine2 = new UciEngine(engineBinary);
        var engineGame = new EngineGame(engine1, engine2, game);
        return engineGame;
    }
}
