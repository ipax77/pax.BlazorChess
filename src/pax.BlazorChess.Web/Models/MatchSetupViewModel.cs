namespace pax.BlazorChess.Web.Models;

public sealed class MatchSetupViewModel : EngineSetupViewModel
{
    public int GameCount { get; set; } = 10;
    public bool ReverseEngines { get; set; } = true;
    public bool RetainCompletedGames { get; set; }
}
