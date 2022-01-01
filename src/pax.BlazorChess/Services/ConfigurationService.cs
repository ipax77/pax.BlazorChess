using pax.BlazorChess.Models;
using pax.uciChessEngine;
using System.Text.Json;

namespace pax.BlazorChess.Services;

public class ConfigurationService
{
    private object lockobject = new object();
    private readonly ILogger<ConfigurationService> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration configuration;

    public UserConfig UserConfig { get; private set; }

    public ConfigurationService(ILogger<ConfigurationService> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.configuration = configuration;
        var configDir = Path.GetDirectoryName(Program.ConfigFile);

        if (String.IsNullOrEmpty(configDir))
        {
            throw new ArgumentException($"config file invalid: {Program.ConfigFile}");
        }

        UserConfig = new UserConfig();

        if (!Directory.Exists(configDir))
        {
            logger.LogInformation($"creating user config dir: {configDir}");
            Directory.CreateDirectory(configDir);
        }

        if (!File.Exists(Program.ConfigFile))
        {
            logger.LogInformation($"creating user config file: {Program.ConfigFile}");
            SaveConfig();
        }
        else
        {
            configuration.GetSection("ChessEngines").Bind(UserConfig.ChessEngines);
        }
    }

    public void SaveConfig()
    {
        lock (lockobject)
        {
            File.WriteAllText(Program.ConfigFile, JsonSerializer.Serialize(UserConfig, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }

    public void SaveConfig(List<ConfigHelper> configHelpers)
    {
        lock (lockobject)
        {
            UserConfig.ChessEngines.Clear();
            foreach (var configHelper in configHelpers)
            {
                if (!String.IsNullOrEmpty(configHelper.Name) && !String.IsNullOrEmpty(configHelper.Path) && File.Exists(configHelper.Path))
                {
                    UserConfig.ChessEngines.Add(configHelper.Name, configHelper.Path);
                }
            }
            File.WriteAllText(Program.ConfigFile, JsonSerializer.Serialize(UserConfig, new JsonSerializerOptions() { WriteIndented = true }));
            using (var scope = scopeFactory.CreateScope())
            {
                var engineService = scope.ServiceProvider.GetRequiredService<EngineService>();
                engineService.UpdateEngines();
            }
        }
    }
}
