using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.Board;
using pax.BlazorChess.Board.Storage;
using pax.BlazorChess.Db;
using pax.BlazorChess.Web.Components;

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

var app = builder.Build();

using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ChessContext>();
context.Database.Migrate();

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
