using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using pax.BlazorChartJs;
using pax.BlazorChess.Board.Storage;

namespace pax.BlazorChess.Board;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChessBoard(this IServiceCollection services)
    {
        services.AddChartJs(options =>
        {
            options.ChartJsLocation = "/_content/pax.BlazorChess.Board/chart.umd.min.js";
            options.ChartJsPluginDatalabelsLocation = "/_content/pax.BlazorChess.Board/chartjs-plugin-datalabels.min.js";
            options.ChartJsCallbacksModuleLocation = "/_content/pax.BlazorChess.Board/chartJsCallbacks.js";
        });

        services.AddScoped<IBoardJsInterop, BoardJsInterop>();
        services.TryAddSingleton<IChessBoardRepository, InMemoryChessBoardRepository>();

        return services;
    }
}
