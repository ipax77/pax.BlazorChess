using ElectronNET.API;
using ElectronNET.API.Entities;
using System.Reflection;

namespace pax.BlazorChess.Services;
public class ElectronService
{
    public static Version CurrentVersion { get; private set; } = new Version(0, 6, 2);
    public static Version AvailableVersion { get; private set; } = new Version(0, 6, 2);
    public static string? AppPath { get; private set; } = String.Empty;

    public event EventHandler<DownloadEventArgs>? DownloadProgress;

    protected virtual void OnDownloadProgress(DownloadEventArgs e)
    {
        EventHandler<DownloadEventArgs>? handler = DownloadProgress;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    public static async Task<string?> GetPath()
    {
        if (HybridSupport.IsElectronActive)
        {
            AppPath = await Electron.App.GetAppPathAsync();
            if (!String.IsNullOrEmpty(AppPath) && AppPath.EndsWith("app.asar"))
                AppPath = AppPath.Substring(0, AppPath.Length - 9);
            AppPath = Path.Combine(AppPath, "bin");
        }
        else
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AppPath = Path.GetDirectoryName(assembly.Location);
        }
        return AppPath;
    }

    public async Task<bool> CheckForUpdate(int delay = 0)
    {
        if (HybridSupport.IsElectronActive)
        {
            try
            {
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
                Electron.AutoUpdater.OnError += (message) => Electron.Dialog.ShowErrorBox("Error", message);
                CurrentVersion = new Version(await Electron.App.GetVersionAsync());
                Electron.AutoUpdater.AutoDownload = false;
                var updateResult = await Electron.AutoUpdater.CheckForUpdatesAsync();
                AvailableVersion = new Version(updateResult.UpdateInfo.Version);
                OnDownloadProgress(new DownloadEventArgs());
                if (AvailableVersion > CurrentVersion)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            } catch (Exception ex)
            {
                Console.WriteLine($"checking for update failed: {ex.Message}");
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public async Task DownloadNewVersion(bool install)
    {
        if (AvailableVersion > CurrentVersion)
        {
            Electron.AutoUpdater.OnDownloadProgress += (info) =>
            {
                OnDownloadProgress(new DownloadEventArgs() { Info = info });
            };

            Electron.AutoUpdater.OnUpdateDownloaded += (info) =>
            {
                OnDownloadProgress(new DownloadEventArgs() { Done = true });
                if (install)
                {
                    Electron.AutoUpdater.QuitAndInstall(true, true);
                }
            };

            if (!install)
            {
                Electron.AutoUpdater.AutoInstallOnAppQuit = true;
            }
            var downloadResult = await Electron.AutoUpdater.DownloadUpdateAsync();
        }
    }

}

public class DownloadEventArgs : EventArgs
{
    public ProgressInfo? Info { get; set; }
    public bool Done { get; set; } = false;
    public double Percent => Info == null ? 0 : Double.Parse(Info.Percent);

}
