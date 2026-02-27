using System.Text.Json;

namespace HorneroZoneIdentifier;

internal sealed class AppSettings
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HorneroZoneIdentifier");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public List<string> MonitoredFolders { get; set; } = [];
    public bool StartWithWindows { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
            }
        }
        catch
        {
            // Corrupt file — use defaults
        }
        return CreateDefault();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }

    private static AppSettings CreateDefault()
    {
        var settings = new AppSettings();

        // Default monitored folders: Desktop, Downloads, Documents
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        foreach (var folder in new[] { desktop, downloads, documents })
        {
            if (Directory.Exists(folder))
                settings.MonitoredFolders.Add(folder);
        }

        settings.Save();
        return settings;
    }
}
