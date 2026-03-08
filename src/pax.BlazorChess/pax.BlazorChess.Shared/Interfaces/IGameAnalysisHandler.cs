using pax.chess;
using pax.uciChessEngine;

namespace pax.BlazorChess.Shared.Interfaces;

public interface IGameAnalysisHandler
{
    GameAnalysis Create(ChessGame game, int threads = 8);
}
