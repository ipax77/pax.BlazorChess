using pax.chess;
using pax.uciChessEngine;

namespace pax.BlazorChess.Services;

public class GameService
{
    public Game Game { get; private set; }
    public Game CurrentGame { get; private set; }
    public List<Game> PvGames { get; private set; } = new List<Game>();
    public Game ActiveGame { get; private set; }

    public GameService()
    {
        Game = new Game();
        CurrentGame = new Game(Game);
        ActiveGame = CurrentGame;
    }

    public void SetActiveGame(Game game)
    {
        ActiveGame = game;
    }

    public void SetCurrentToPv()
    {
        Game game = new(CurrentGame);
        game.Name = "Current";
        PvGames.Add(game);
    }

    public void SetPvGame(PvInfo pvInfo, string engineName)
    {
        Game game = new(CurrentGame);
        game.Name = $"{engineName} PV{pvInfo.MultiPv}";
        for (int i = 0; i < pvInfo.Moves.Count; i++)
        {
            game.Move(pvInfo.Moves[i]);

        }
        game.ObserverMoveTo(game.State.Moves.Count);
        PvGames.Add(game);
    }

    public void RemovePvGame(Game game)
    {
        PvGames.Remove(game);
    }

    public void SetPvGamePositionToCurrent(Game pvgame)
    {
        Game game = new Game();
        for (int i = 0; i < pvgame.ObserverMove; i++)
        {
            game.Move(pvgame.State.Moves[i]);
        }
        Game = new(CurrentGame);
        CurrentGame = game;
    }

    public void ResetCurrentGame()
    {
        CurrentGame = new Game(Game);
    }
}
