// See https://aka.ms/new-console-template for more information
using pax.chess;
using pax.uciChessEngine;

Console.WriteLine("Hello, World!");

int bab = 0;
for (int i = 0; i < 10; i++)
{
    bab = Test().GetAwaiter().GetResult();
}

Console.ReadLine();
Console.WriteLine($"done: {bab}");

public partial class Program
{

    public static async Task<int> Test()
    {
        Game game = DbMap.GetGame("d2d4e7e6c2c4c7c6b1c3d7d5g1f3g8f6c1g5f8e7e2e3b8d7d1c2h7h6g5h4e8g8f1d3d8c7g2g4e6e5c4d5c6d5d4e5d7e5f3e5c7e5e1c1c8g4d1g1g4f3h2h3f3h1g1h1a8c8c1b1d5d4e3d4e5d4h1g1d4h4c2e2f8e8g1g4f6g4e2e4g4f6e4h4e7b4h4b4c8d8a2a3d8d3b4b7e8d8b1a2d3d7b7f3f6d5c3d5d7d5f3e4d5d2e4e7g8h7e7a7d2f2a7f2d8d2");
        GameAnalyzes ga = new GameAnalyzes(game, new KeyValuePair<string, string>("Stockfish", @"C:\data\stockfish_14.1_win_x64_avx2\stockfish_14.1_win_x64_avx2.exe"));

        CancellationTokenSource tokenSource = new CancellationTokenSource();
        await foreach (var bab in ga.Analyze(tokenSource.Token))
        {
        }

        return ga.Done;

    }
}
