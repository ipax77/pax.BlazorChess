using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.Models;
using pax.BlazorChess.Services;
using pax.uciChessEngine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    // config.Sources.Clear();
    config.AddJsonFile(ConfigFile, optional: true, reloadOnChange: true);
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContext<ChessContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pax.chess", "chess.db")}",
       x =>
       {
           x.MigrationsAssembly("pax.BlazorChess");
           x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
       }
    )
//.EnableSensitiveDataLogging()
//.EnableDetailedErrors()
);

builder.Services.AddScoped<DbService>();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<EngineService>();

var app = builder.Build();

// warmup
app.Services.GetRequiredService<ConfigurationService>();

using (var scope = app.Services.CreateScope())
{
    // var context = scope.ServiceProvider.GetService<ChessContext>();
    // context?.Database.Migrate();

    // var dbService = scope.ServiceProvider.GetService<DbService>();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public partial class Program
{
    public static string ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pax.chess", "config.json");
}