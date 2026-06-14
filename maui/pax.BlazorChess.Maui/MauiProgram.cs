using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.BlazorChess.Board;
using pax.BlazorChess.Board.Storage;
using pax.BlazorChess.Db;
using pax.BlazorChess.Maui.Services;

namespace pax.BlazorChess.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif
            var sqliteDirectory = Path.Combine(FileSystem.AppDataDirectory, "chess");
            Directory.CreateDirectory(sqliteDirectory);
            var sqliteConnectionString = $"Data Source={Path.Combine(sqliteDirectory, "blazorChess.db")}";
            builder.Services.AddDbContext<ChessContext>(options => options
                .UseSqlite(sqliteConnectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("pax.BlazorChess.Db");
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                }));
            builder.Services.AddScoped<IChessBoardRepository, EfChessBoardRepository>();
            builder.Services.Configure<AnalysisPersistenceOptions>(builder.Configuration.GetSection("AnalysisPersistence"));
            builder.Services.AddChessBoard();
            builder.Services.AddScoped<AnalysisWorkspaceState>();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChessContext>();
                context.Database.Migrate();
            }

            return app;
        }
    }
}
