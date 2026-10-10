namespace NetworkActivityTray;

internal static class StoragePaths
{
    public static bool PortableMode =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.txt"));

    public static string SettingsDirectory =>
        PortableMode
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NetworkActivityTray");

    public static string LogDirectory =>
        PortableMode
            ? Path.Combine(AppContext.BaseDirectory, "Data", "Logs")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NetworkActivityTray",
                "Logs");
}
