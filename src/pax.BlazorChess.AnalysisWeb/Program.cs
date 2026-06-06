using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.AnalysisWeb.Components;
using pax.BlazorChess.AnalysisWeb.Services;
using pax.BlazorChess.Board;
using pax.BlazorChess.Board.Storage;
using pax.BlazorChess.Db;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionDirectory = Path.Combine(builder.Environment.ContentRootPath, "obj", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionDirectory);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var sqliteDirectory = "/data/chess";
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
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChessContext>();
    await context.Database.MigrateAsync();
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
