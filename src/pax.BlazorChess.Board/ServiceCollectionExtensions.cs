using Microsoft.Extensions.DependencyInjection;
using pax.uciChessEngine;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Board;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChessBoard(this IServiceCollection services)
    {
        services.AddScoped<IBoardJsInterop, BoardJsInterop>();

        //if (!services.Any(s => s.ServiceType == typeof(IChessBoardRepository)))
        //{
        //    throw new InvalidOperationException(
        //        "No IChessBoardRepository implementation registered. " +
        //        "Please register one before calling AddChessBoard().");
        //}
        services.AddSingleton<IChessBoardRepository, ChessBoardRepository>();
        return services;
    }
}

public interface IChessBoardRepository
{
    Task<List<EngineRunOptions>> GetEngineRunOptions();
    Task StoreEngineRunOptions(List<EngineRunOptions> engineRunOptions);
}

// DEBUG
public class ChessBoardRepository : IChessBoardRepository
{
    private readonly List<EngineRunOptions> engineRunOptions;

    public ChessBoardRepository()
    {
        engineRunOptions = [
            new() {
                BinaryPath = @"C:\data\chess\engines\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe",
                Name = "Stockfish 18",
                Threads = 4,
                Pvs = 2,
                PoolSize = 8,
            },
            new() {
                BinaryPath = @"C:\data\chess\engines\lc0-v0.32.1-windows-gpu-nvidia-cuda11\lc0.exe",
                Name = "LC0 v0.32.1",
                Threads = 2,
                Pvs = 2,
                PoolSize = 2,
            }
        ];
    }

    public async Task<List<EngineRunOptions>> GetEngineRunOptions()
    {
        return engineRunOptions;
    }

    public async Task StoreEngineRunOptions(List<EngineRunOptions> options)
    {
        engineRunOptions.Clear();
        engineRunOptions.AddRange(options);
    }
}