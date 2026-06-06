using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.Board;
using pax.BlazorChess.Board.Storage;
using pax.BlazorChess.Db;
using pax.BlazorChess.Web.Components;
using pax.BlazorChess.Web.Services;
using pax.uciChessEngine.EngineServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var sqliteConnectionString = $"Data Source={Path.Combine("/data/chess", "blazorChess.db")}";
builder.Services.AddDbContext<ChessContext>(options => options
    .UseSqlite(sqliteConnectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("pax.BlazorChess.Db");
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    })
//.EnableDetailedErrors()
//.EnableSensitiveDataLogging()
);

builder.Services.AddScoped<IChessBoardRepository, EfChessBoardRepository>();
builder.Services.AddChessBoard();
builder.Services.AddScoped<PositionWorkspaceState>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ChessContext>();
context.Database.Migrate();

// DEBUG
if (context.EngineRunOptions.Count() == 0)
{
    List<EngineRunOptions> engineRunOptions =
    [
        new()
        {
            BinaryPath = @"C:\data\chess\engines\stockfish-windows-x86-64-avx2\stockfish\stockfish-windows-x86-64-avx2.exe",
            Name = "Stockfish 18",
            Threads = 4,
            Pvs = 4,
            PoolSize = 8,
        },
        new()
        {
            BinaryPath = @"C:\data\chess\engines\lc0-v0.32.1-windows-gpu-nvidia-cuda11\lc0.exe",
            Name = "LC0 v0.32.1",
            Threads = 2,
            Pvs = 2,
            PoolSize = 2,
        }
    ];
    var repo = scope.ServiceProvider.GetRequiredService<IChessBoardRepository>();
    await repo.StoreEngineRunOptions(engineRunOptions);
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
