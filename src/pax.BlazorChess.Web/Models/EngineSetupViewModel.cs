namespace pax.BlazorChess.Web.Models;

public class EngineSetupViewModel
{
    public Guid WhiteEngineId { get; set; }
    public Guid BlackEngineId { get; set; }
    public int BaseMinutes { get; set; } = 5;
    public int IncrementSeconds { get; set; } = 3;
    public int ThinkTimePerMoveMs { get; set; } = 1000;
}
