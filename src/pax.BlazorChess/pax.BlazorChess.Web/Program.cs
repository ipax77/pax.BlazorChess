using pax.BlazorChartJs;
using pax.BlazorChess.Shared.Interfaces;
using pax.BlazorChess.Web.Components;
using pax.BlazorChess.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddChartJs();

builder.Services.AddScoped<IEngineGameHandler, EngineGameHandler>();
builder.Services.AddScoped<IGameAnalysisHandler, GameAnalysisHandler>();

var app = builder.Build();

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
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(pax.BlazorChess.Shared._Imports).Assembly);

app.Run();


