namespace pax.BlazorChess.Models;

public class Settings
{
}

public class ReviewSettings
{
    public string EngineString { get; set; } = String.Empty;
    public int MsPerMove { get; set; } = 2000;
    public int Threads { get; set; } = Environment.ProcessorCount / 2;
}