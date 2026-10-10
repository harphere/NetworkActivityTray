namespace NetworkActivityTray;

internal static class AppLog
{
    private static readonly object Sync = new();
    private const long MaxLogBytes = 1_000_000;
    private const int BackupCount = 3;

    public static string LogDirectory => StoragePaths.LogDirectory;
    public static string LogPath => Path.Combine(LogDirectory, "NetworkActivityTray.log");

    public static bool VerboseEnabled { get; set; }

    public static void Info(string message) => Write("INFO", message);

    public static void Debug(string message)
    {
        if (VerboseEnabled)
            Write("DEBUG", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        if (ex is not null)
            message += Environment.NewLine + ex;
        Write("ERROR", message);
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();

                string line =
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
            return;

        if (new FileInfo(LogPath).Length < MaxLogBytes)
            return;

        string oldest = $"{LogPath}.{BackupCount}";
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (int i = BackupCount - 1; i >= 1; i--)
        {
            string source = $"{LogPath}.{i}";
            string dest = $"{LogPath}.{i + 1}";
            if (File.Exists(source))
                File.Move(source, dest, overwrite: true);
        }

        File.Move(LogPath, $"{LogPath}.1", overwrite: true);
    }
}
