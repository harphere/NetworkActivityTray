namespace NetworkActivityTray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLog.Error("Unhandled exception", e.ExceptionObject as Exception);

        Application.ThreadException += (_, e) =>
            AppLog.Error("UI thread exception", e.Exception);

        AppLog.Info("Network Activity Tray 0.5 starting");
        Application.Run(new TrayApplicationContext());
        AppLog.Info("Network Activity Tray stopped");
    }
}
