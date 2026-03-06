using pax.chess;
using pax.uciChessEngine;

namespace pax.BlazorChess.Shared.Interfaces;

public interface IEngineGameHandler
{
    IEngineGame GetEngineGame(ChessGame game);
}
