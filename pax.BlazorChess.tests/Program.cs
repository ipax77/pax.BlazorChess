// See https://aka.ms/new-console-template for more information
using pax.chess;
using pax.uciChessEngine;
using static pax.chess.Pgn;

Console.WriteLine("Hello, World!");

// int bab = Test().GetAwaiter().GetResult();
int bab = 0;

var pgnFile = @"C:\data\pgns\lichess_study_game-2_magnus-carlsen-ian-nepomniachtchi_by_cFlour_2021.11.27.pgn";
var lines = File.ReadAllLines(pgnFile);

var game = Pgn.MapStrings(lines);

Console.WriteLine(game.State.Moves.Count);

Console.ReadLine();
Console.WriteLine($"done: {bab}");

public partial class Program
{

    public static async Task<int> Test()
    {
        Game game = DbMap.GetGame("d2d4e7e6c2c4c7c6b1c3d7d5g1f3g8f6c1g5f8e7e2e3b8d7d1c2h7h6g5h4e8g8f1d3d8c7g2g4e6e5c4d5c6d5d4e5d7e5f3e5c7e5e1c1c8g4d1g1g4f3h2h3f3h1g1h1a8c8c1b1d5d4e3d4e5d4h1g1d4h4c2e2f8e8g1g4f6g4e2e4g4f6e4h4e7b4h4b4c8d8a2a3d8d3b4b7e8d8b1a2d3d7b7f3f6d5c3d5d7d5f3e4d5d2e4e7g8h7e7a7d2f2a7f2d8d2");

        Engine engine = new Engine("Lc0", "C:\\data\\lc0-v0.28.0-windows-gpu-nvidia-cuda-nodll\\lc0.exe");
        engine.Start();
        await engine.IsReady();
        engine.Send("ucinewgame");
        await engine.IsReady();
        // _ = await engine.GetOptions();
        // engine.SetOption("Threads", 2);
        // await engine.IsReady();
        engine.Send("go");
        await Task.Delay(5000);
        engine.Send("stop");
        return 0;
    }
}
