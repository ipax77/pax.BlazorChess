using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.Models;
using pax.chess;

namespace pax.BlazorChess.Services;

public class DbService
{
    private readonly ChessContext context;
    private readonly ILogger<DbService> logger;

    public DbService(ChessContext context, ILogger<DbService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task SaveGame(Game game, int gameId)
    {
        var dbGame = await context.Games.FirstOrDefaultAsync(f => f.Id == gameId);
        if (dbGame == null) return;

        DbMap.SetGameInfo(dbGame, game);
        dbGame.HalfMoves = game.State.Moves.Count;
        dbGame.EngineMoves = String.Concat(game.State.Moves.Select(s => Map.GetEngineMoveString(s)));
        dbGame.Variations = DbMap.GetVariations(game);
        await context.SaveChangesAsync();
    }

    public async Task<Game?> GetGameFromIdAsync(int gameId)
    {
        var dbGame = await context.Games
            .Include(i => i.Variations)
                .ThenInclude(i => i.Evaluation)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == gameId);
        if (dbGame == null) return null;
        var game = DbMap.GetGame(dbGame);
        return game;
    }

    public async Task<int> GetGamesCountAsync(GameRequest request)
    {
        return await context.Games.CountAsync();
    }

    public async Task<List<DbGame>> GetGamesAsync(GameRequest request, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var games = context.Games.AsNoTracking();

                if (request.SortOrders.Any())
                {
                    foreach (var sortOrder in request.SortOrders)
                    {
                        games = sortOrder.Order ?
                            games = games.AppendOrderBy(sortOrder.Sort)
                            : games.AppendOrderByDescending(sortOrder.Sort);
                    }
                }
                else
                {
                    games = games.OrderBy(o => o.UTCDate);
                }

                return await games.Skip(request.StartIndex).Take(request.Count).ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
        return new List<DbGame>();
    }

    public async Task<int> GetSamePosGamesCountAsync(uint zobrist)
    {
        DbPosition? pos = await context.Positions.Include(i => i.Games).AsNoTracking().FirstOrDefaultAsync(f => f.Position == zobrist);
        if (pos != null)
        {
            return pos.Games.Count;
        }
        return 0;
    }

    public async Task<List<DbGame>> GetSamePosGamesAsync(uint zobrist, int startIndex, int count, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                DbPosition? pos = await context.Positions.Include(i => i.Games).AsNoTracking().FirstOrDefaultAsync(f => f.Position == zobrist, cancellationToken);
                if (pos != null)
                {
                    return pos.Games.ToList();
                }
            }
            catch (OperationCanceledException) { }
        }
        return new List<DbGame>();
    }

    public void SetPositions()
    {
        var games = context.Games.Include(i => i.Positions).Where(x => x.Variant == Variant.Standard && !x.Positions.Any()).OrderBy(x => x.Id);
        List<DbPosition> positions = context.Positions.Include(i => i.Games).ToList();

        int c = 0;
        foreach (var dbGame in games)
        {
            var game = DbMap.GetGame(dbGame, dbGame.Site);
            for (int i = 0; i < game.State.Moves.Count; i++)
            {
                game.ObserverMoveForward();
                var hash = Zobrist.GetHashCode(game.ObserverState);
                DbPosition? pos = positions.FirstOrDefault(f => f.Position == hash);
                if (pos == null)
                {
                    pos = new DbPosition()
                    {
                        Position = hash
                    };
                    positions.Add(pos);
                    context.Positions.Add(pos);
                }
                if (!pos.Games.Contains(dbGame))
                {
                    pos.Games.Add(dbGame);
                }
                if (!dbGame.Positions.Contains(pos))
                {
                    dbGame.Positions.Add(pos);
                }
            }
            c++;
            if (c % 100 == 0)
            {
                context.SaveChanges();
            }
        }
        context.SaveChanges();
    }

    public void ImportTest()
    {
        if (context.Games.Any())
        {
            return;
        }
        var pgnFile = @"C:\data\pgns\lichess_pax77_2021-12-14.pgn";

        var lines = File.ReadAllLines(pgnFile);
        var pgnLines = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            pgnLines.Add(lines[i]);
            if (lines[i].StartsWith("1. "))
            {
                Game game = Pgn.MapStrings(pgnLines.ToArray());
                DbGame dbGame = DbMap.GetGame(game);
                context.Games.Add(dbGame);
                pgnLines.Clear();
            }
            if (i % 100 == 0)
            {
                context.SaveChanges();
            }
        }
        context.SaveChanges();
    }

    public Game ConvertTest()
    {
        // var dbGame = context.Games.AsNoTracking().Where(x => x.EngineMoves.EndsWith("Q")).First();
        var dbGame = context.Games.AsNoTracking().Where(x => x.Site == "https://lichess.org/58vKeuQy").First();
        var game = DbMap.GetGame(dbGame);
        logger.LogInformation("got game");
        return game;
    }
}
