namespace pax.BlazorChess.Models;

public class UserConfig
{
    public Dictionary<string, string> ChessEngines { get; set; } = new Dictionary<string, string>();
}

public class ConfigHelper
{
    public string Name { get; set; } = String.Empty;
    public string Path { get; set; } = String.Empty;
}