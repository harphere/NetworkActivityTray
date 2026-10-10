using System.Text.Json;

namespace NetworkActivityTray;

internal sealed class AppSettings
{
    public string? AdapterId { get; set; }
    public int PollingIntervalMs { get; set; } = 350;
    public long SendThresholdBytesPerSecond { get; set; } = 1024;
    public long ReceiveThresholdBytesPerSecond { get; set; } = 1024;
    public bool StartWithWindows { get; set; }
    public bool EnableVerboseLogging { get; set; }

    public static string SettingsPath =>
        Path.Combine(StoragePaths.SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            string json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

            settings.PollingIntervalMs = Math.Clamp(settings.PollingIntervalMs, 100, 2000);
            settings.SendThresholdBytesPerSecond = Math.Max(0, settings.SendThresholdBytesPerSecond);
            settings.ReceiveThresholdBytesPerSecond = Math.Max(0, settings.ReceiveThresholdBytesPerSecond);

            return settings;
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not load settings; using defaults", ex);
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(StoragePaths.SettingsDirectory);

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(SettingsPath, json);
    }
}
