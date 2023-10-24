using Microsoft.AspNetCore.Components;
using pax.BlazorChess.Models;
using pax.BlazorChess.Services;
using pax.BlazorChess.Shared;
using pax.chess;
using pax.uciChessEngine;
using Blazored.Toast.Services;
using pax.BlazorChartJs;

namespace pax.BlazorChess.Pages;
public partial class EngineAnalyzesPage : ComponentBase, IDisposable
{
# nullable disable
    [Inject]
    protected EngineService engineService { get; set; }
    [Inject]
    protected DbService dbService { get; set; }
    [Inject]
    protected NavigationManager _nav { get; set; }
    [Inject]
    protected IToastService toastService { get; set; }
    [Inject]
    protected ILogger<EngineAnalyzesPage> logger { get; set; }

# nullable enable

    [Parameter]
    [SupplyParameterFromQuery]
    public int? GameId { get; set; }

    List<GameAnalyzes> GameAnalyzis = new List<GameAnalyzes>();
    GameAnalyzes? Analysis;
    BoardContainer? boardContainer;
    ChartComponent? chartComponent;
    LoadModal? loadModal;
    SettingsModal? settingModal;
    EngineComponent? engineComponent;
    MoveStatsComponent? moveStatsComponent;

    CancellationTokenSource cts = new CancellationTokenSource();

    private ChartJsConfig chartConfig = ChartService.GetRatingChart();
    private bool Analyzing = false;
    private bool Loading = false;

    private ReviewSettings reviewSettings = new ReviewSettings();

    private Dictionary<int, List<Variation>> ReviewVariations = new Dictionary<int, List<Variation>>();

    private int currentMoveId => Analysis == null ? 0 : Analysis.Game.ObserverState.CurrentMove == null ? 0 : Analysis.Game.ObserverState.CurrentMove.Variation != null ? 0 : Analysis.Game.ObserverState.CurrentMove.HalfMoveNumber;
    private List<Variation> reviewVariations = new List<Variation>();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        reviewSettings.EngineString = engineService.AvailableEngines.First().Key;
        _ = Init();
    }

    private async Task Init()
    {
        GameAnalyzis = engineService.GetGameAnalyses().ToList();
        if (GameId == null)
        {
            Analysis = GameAnalyzis.FirstOrDefault();
        }
        else
        {
            var game = await dbService.GetGameFromIdAsync((int)GameId);
            if (game != null)
            {
                Analysis = engineService.CreateGameAnalyzes(game, reviewSettings.EngineString);
            }
        }

        if (Analysis != null)
        {
            chartConfig.SetLabels(Analysis.Game.State.Moves.Select(s => s.HalfMoveNumber.ToString()).ToList());
            if (Analysis.Game.ReviewVariations.Any())
            {
                ReviewVariations = new Dictionary<int, List<Variation>>(Analysis.Game.ReviewVariations);
            }
        }
        await InvokeAsync(() => StateHasChanged());
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            UpdateChart();
            if (Analysis != null)
            {
                moveStatsComponent?.Update(Analysis.Game);
            }
        }
    }

    private async Task GameImport(Game game)
    {
        GameId = null;
        Loading = true;
        await InvokeAsync(() => StateHasChanged());

        if (Analysis != null)
        {
            engineService.DeleteGameAnalyzes(Analysis);
            Analysis = null;

        }

        if (engineComponent != null)
        {
            await engineComponent.Reset(game);
        }

        Analysis = engineService.CreateGameAnalyzes(game, reviewSettings.EngineString);

        if (game.State.Moves.Any())
        {
            if (!game.Infos.ContainsKey("UTCDate"))
            {
                game.Infos["UTCDate"] = DateTime.UtcNow.ToString("yyyy.MM.dd");
                game.Infos["UTCTime"] = DateTime.UtcNow.ToString("HH:mm:ss");
            }
            var dbGame = await dbService.SaveGame(game, null);
            if (dbGame != null)
            {
                GameId = dbGame.Id;
                _nav.NavigateTo(_nav.GetUriWithQueryParameter("GameId", GameId));
            }
        }
        chartConfig.SetLabels(Analysis.Game.State.Moves.Select(s => s.HalfMoveNumber.ToString()).ToList());
        ReviewVariations.Clear();
        reviewVariations.Clear();
        UpdateChart();
        Loading = false;
        await InvokeAsync(() => StateHasChanged());
    }

    private void SettingsChoosen()
    {
        if (Analysis != null)
        {
            engineService.DeleteGameAnalyzes(Analysis);
            Analysis = engineService.CreateGameAnalyzes(Analysis.Game, reviewSettings.EngineString);
        }
    }

    private void ChartClicked(ChartJsEvent chartJsEvent)
    {
        if (chartJsEvent is ChartJsLabelClickEvent clickEvent)

            if (Analysis != null)
            {
                Analysis.Game.ObserverMoveTo(Convert.ToInt32(clickEvent.DataX));
                ObserverMoveChanged();
                boardContainer?.Focus();
            }
    }

    private void UpdateChart(bool dry = false)
    {
        if (Analysis != null)
        {
            List<object> data = new();

            for (int i = 0; i < Analysis.Game.State.Moves.Count; i++)
            {
                if (ReviewVariations.ContainsKey(i) && ReviewVariations[i].Any())
                {
                    var chartScore = ReviewVariations[i].OrderBy(o => o.Pv).First().Evaluation?.ChartScore();
                    data.Add(chartScore == null ? 0 : (double)chartScore);
                }
                else
                {
                    data.Add(0);
                }
            }
            if (!dry && chartConfig.Data.Datasets.Any())
            {
                chartConfig.Data.Datasets.First().Data = data;
                chartConfig.ReinitializeChart();
            }
        }
    }

    private void ObserverMoveChanged()
    {
        if (Analysis != null && Analysis.Game.ObserverState.CurrentMove != null)
        {
            var arConfig = chartConfig.Options?.Plugins?.ArbitraryLines?.FirstOrDefault();
            if (Analysis.Game.ObserverState.CurrentMove.Variation == null)
            {
                if (arConfig != null)
                {
                    arConfig.XPosition = Analysis.Game.ObserverState.CurrentMove.HalfMoveNumber;
                    chartComponent?.UpdateChartOptions();
                }
            }
            else
            {
                var startMove = Analysis.Game.ObserverState.CurrentMove.Variation?.StartMove;
                if (arConfig != null)
                {
                    arConfig.XPosition = startMove ?? 0;
                    chartComponent?.UpdateChartOptions();
                }
            }
            boardContainer?.DrawReviewHints();
            reviewVariations = Analysis.Game.GetCurrentReviewVariations().ToList();
            // nextVariation = !Analysis.Game.ReviewVariations.ContainsKey(currentMoveId + 1) ? null : Analysis.Game.ReviewVariations[currentMoveId + 1].OrderBy(o => o.Pv).FirstOrDefault();
        }
        _ = engineComponent?.Update();
    }

    private MoveQuality ValidateMove(Move move, Variation? bestVariation, Variation? runnerVariation)
    {
        MoveQuality moveQuality = MoveQuality.Unknown;

        if (move.Evaluation == null || bestVariation == null || bestVariation.Evaluation == null)
        {
            return MoveQuality.Unknown;
        }

        if (runnerVariation == null || runnerVariation.Evaluation == null)
        {
            return MoveQuality.Only;
        }

        if (bestVariation != null)
        {
            if (move.EngineMove == bestVariation.Moves.FirstOrDefault()?.EngineMove)
            {
                return MoveQuality.Best;
            }

            double diff = Math.Abs(move.Evaluation.ChartScore() - bestVariation.Evaluation.ChartScore());
            // double runnerDiff = Math.Abs(move.Evaluation.ChartScore() - runnerVariation.Evaluation.ChartScore());

            if (diff == 0)
            {
                return MoveQuality.Best;
            }

            double eval = Math.Abs(move.Evaluation.ChartScore());


            if (move.EngineMove == runnerVariation.Moves.FirstOrDefault()?.EngineMove)
            {
                if (diff < 0.5 * Math.Max(1.0, eval))
                {
                    return MoveQuality.Runner;
                }
            }

            if (diff > Math.Max(1.0, eval * 0.5))
            {
                return MoveQuality.Blunder;
            }

            if (diff >= 0.5 * Math.Max(1.0, eval))
            {
                return MoveQuality.Questionmark;
            }

            return MoveQuality.Clubhouse;

        }
        return moveQuality;
    }

    private async Task Analyse()
    {
        int i = 0;
        if (Analysis != null)
        {
            chartConfig.SetLabels(Analysis.Game.State.Moves.Select(s => s.HalfMoveNumber.ToString()).ToList());
            Analyzing = true;
            await InvokeAsync(() => StateHasChanged());
            cts = new CancellationTokenSource();
            ReviewVariations = new Dictionary<int, List<Variation>>();
            Analysis.Game.ReviewVariations.Clear();

            try
            {
                await foreach (var variation in Analysis.Analyze(cts.Token, TimeSpan.FromMilliseconds(reviewSettings.MsPerMove), 2, reviewSettings.Threads))
                {
                    if (!ReviewVariations.ContainsKey(variation.StartMove))
                    {
                        ReviewVariations[variation.StartMove] = new List<Variation>();
                    }
                    ReviewVariations[variation.StartMove].Add(variation);

                    i++;
                    if (i % 20 == 0)
                    {
                        UpdateChart();
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                UpdateChart();
                Analyzing = false;

                foreach (var ent in ReviewVariations.OrderBy(o => o.Key))
                {
                    Analysis.Game.ReviewVariations[ent.Key] = ent.Value.OrderBy(o => o.Pv).ToList();
                    if (ent.Key - 1 >= 0)
                    {
                        var move = Analysis.Game.State.Moves[ent.Key - 1];
                        var eval = Analysis.Game.ReviewVariations[ent.Key].First().Evaluation;
                        move.Evaluation = eval;
                        if (eval != null)
                        {
                            eval.MoveQuality =
                                ValidateMove(move, Analysis.Game.ReviewVariations[ent.Key - 1].FirstOrDefault(), Analysis.Game.ReviewVariations[ent.Key - 1].Count > 1 ? Analysis.Game.ReviewVariations[ent.Key - 1][1] : null);
                        }
                    }
                }
                var lastMove = Analysis.Game.State.Moves.LastOrDefault();
                if (lastMove != null)
                {
                    var lastVariation = Analysis.Game.ReviewVariations.OrderBy(o => o.Key).LastOrDefault().Value.FirstOrDefault();
                    var lastEval = lastVariation?.Evaluation;
                    if (lastEval != null)
                    {
                        lastMove.Evaluation = lastEval;
                        lastMove.Evaluation.MoveQuality = ValidateMove(lastMove, lastVariation, Analysis.Game.ReviewVariations.OrderBy(o => o.Key).LastOrDefault().Value.LastOrDefault());
                    }
                }

                toastService.ShowSuccess("Anlysis complete.");
                moveStatsComponent?.Update(Analysis.Game);
                await InvokeAsync(() => StateHasChanged());
            }
        }
    }

    private void ShowLine(int i)
    {
        if (Analysis != null)
        {
            int startMoveId = Analysis.Game.ObserverState.CurrentMove == null ? 0 : Analysis.Game.ObserverState.CurrentMove.HalfMoveNumber;
            if (Analysis.Game.ReviewVariations.ContainsKey(startMoveId))
            {
                var variation = Analysis.Game.ReviewVariations[startMoveId].ElementAt(i);
                if (variation != null)
                {
                    Analysis.Game.CreateVariation(startMoveId, variation.Moves.Select(s => s.EngineMove).ToArray(), variation.Evaluation);
                }
            }
        }
    }

    private async void Save()
    {
        if (Analysis != null)
        {
            var dbGame = await dbService.SaveGame(Analysis.Game, GameId);
            if (dbGame != null)
            {
                toastService.ShowSuccess("Game saved.");
            }
        }
    }

    private async Task MoveRequested()
    {
        if (engineComponent != null)
        {
            var move = await engineComponent.ExecuteBestMove();
            if (move != null)
            {
                boardContainer?.DrawHints(new List<EngineMove>(), true);
                boardContainer?.DrawOtherHints(new List<EngineMove>(), true);
                await InvokeAsync(() => StateHasChanged());
            }
        }
    }

    private void MoveToQuality(KeyValuePair<MoveQuality, bool> request)
    {
        if (Analysis != null)
        {
            int skip = Analysis.Game.ObserverState.CurrentMove == null ? 0 : Analysis.Game.ObserverState.CurrentMove.HalfMoveNumber + 1;
            var move = Analysis.Game.State.Moves.Skip(skip).Where(x => (request.Value ? x.HalfMoveNumber % 2 != 0 : x.HalfMoveNumber % 2 == 0)).FirstOrDefault(f => f.Evaluation?.MoveQuality == request.Key);
            if (move == null)
            {
                move = Analysis.Game.State.Moves.Where(x => (request.Value ? x.HalfMoveNumber % 2 != 0 : x.HalfMoveNumber % 2 == 0)).FirstOrDefault(f => f.Evaluation?.MoveQuality == request.Key);
            }
            if (move != null)
            {
                Analysis.Game.ObserverMoveTo(move);
                ObserverMoveChanged();
            }
        }
    }
    private void StartEngineVsEngineGame()
    {
        if (Analysis != null)
        {
            Game eveGame = new Game();
            for (int i = 0; i < Analysis.Game.ObserverState.Moves.Count; i++)
            {
                eveGame.Move(Analysis.Game.ObserverState.Moves[i].EngineMove);
            }
            engineService.CreateEngineGame(eveGame, "Test1");
            _nav.NavigateTo("enginevsengine");
        }
    }

    private void Stop()
    {
        cts.Cancel();
    }

    public void Dispose()
    {
        Loading = true;
        InvokeAsync(() => StateHasChanged());
        cts.Cancel();
    }
}

