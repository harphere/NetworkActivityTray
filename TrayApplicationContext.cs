using System.Diagnostics;

namespace NetworkActivityTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly AppSettings _settings;
    private readonly NetworkMonitor _monitor;
    private readonly Dictionary<ActivityState, Icon> _icons;
    private readonly PenmIconProvider _penmIcons;

    private ActivityState _lastState = (ActivityState)(-1);
    private ActivityState? _forcedTestState;

    private readonly ToolStripMenuItem _adapterStatusItem;
    private readonly ToolStripMenuItem _testMenu;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        AppLog.VerboseEnabled = _settings.EnableVerboseLogging;

        string iclPath = Path.Combine(AppContext.BaseDirectory, "PENetwork_W10_GUI_Colored.icl");
        _penmIcons = new PenmIconProvider(iclPath);

        _icons = Enum.GetValues<ActivityState>()
            .ToDictionary(
                state => state,
                state => _penmIcons.Get(state) is Icon icon
                    ? (Icon)icon.Clone()
                    : IconRenderer.Create(state));

        _monitor = new NetworkMonitor(
            () => _settings.AdapterId,
            () => _settings.SendThresholdBytesPerSecond,
            () => _settings.ReceiveThresholdBytesPerSecond);

        var menu = new ContextMenuStrip();

        menu.Items.Add("Network Activity Tray v0.5").Enabled = false;
        menu.Items.Add(_penmIcons.IsAvailable ? "Icons: PENM Win10 GUI Colored" : "Icons: built-in fallback").Enabled = false;
        menu.Items.Add(StoragePaths.PortableMode ? "Mode: Portable" : "Mode: Standard").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());

        _adapterStatusItem = new ToolStripMenuItem("Adapter: detecting…") { Enabled = false };
        menu.Items.Add(_adapterStatusItem);

        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        _testMenu = BuildTestMenu();
        menu.Items.Add(_testMenu);

        var logItem = new ToolStripMenuItem("Open Log Folder");
        logItem.Click += (_, _) => OpenLogFolder();
        menu.Items.Add(logItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Network Activity Tray",
            ContextMenuStrip = menu,
            Icon = _icons[ActivityState.Idle]
        };

        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Clamp(_settings.PollingIntervalMs, 100, 2000)
        };

        _timer.Tick += (_, _) => OnTick();
        _timer.Start();

        SyncStartupSetting();
        UpdateTestChecks();
    }

    private ToolStripMenuItem BuildTestMenu()
    {
        var test = new ToolStripMenuItem("Test Icons");
        AddTestItem(test, "Automatic / Live", null);
        test.DropDownItems.Add(new ToolStripSeparator());
        AddTestItem(test, "Idle", ActivityState.Idle);
        AddTestItem(test, "Send", ActivityState.Send);
        AddTestItem(test, "Receive", ActivityState.Receive);
        AddTestItem(test, "Send + Receive", ActivityState.Both);
        AddTestItem(test, "Disconnected", ActivityState.Disconnected);
        return test;
    }

    private void AddTestItem(ToolStripMenuItem parent, string text, ActivityState? state)
    {
        var item = new ToolStripMenuItem(text) { Tag = state };
        item.Click += (_, _) =>
        {
            _forcedTestState = state;
            UpdateTestChecks();
            _lastState = (ActivityState)(-1);
            OnTick();
        };
        parent.DropDownItems.Add(item);
    }

    private void UpdateTestChecks()
    {
        foreach (ToolStripItem raw in _testMenu.DropDownItems)
        {
            if (raw is not ToolStripMenuItem item)
                continue;

            item.Checked = _forcedTestState is null
                ? item.Tag is null
                : item.Tag is ActivityState state && state == _forcedTestState;
        }
    }

    private void OnTick()
    {
        ActivityState liveState = _monitor.Sample();
        ActivityState displayState = _forcedTestState ?? liveState;

        _adapterStatusItem.Text =
            _settings.AdapterId is null
                ? $"Adapter: {_monitor.AdapterName} (Automatic)"
                : $"Adapter: {_monitor.AdapterName}";

        if (displayState != _lastState)
        {
            _notifyIcon.Icon = _icons[displayState];
            AppLog.Debug(
                $"State {displayState}; TX={_monitor.LastSendBytesPerSecond:N0} B/s; " +
                $"RX={_monitor.LastReceiveBytesPerSecond:N0} B/s");
            _lastState = displayState;
        }

        string mode = _forcedTestState is null
            ? StateText(liveState)
            : $"TEST: {StateText(_forcedTestState.Value)}";

        _notifyIcon.Text = TruncateTooltip(
            $"Network Activity Tray\n{_monitor.AdapterName}\n{mode}");
    }

    private void OpenSettings()
    {
        var adapters = _monitor.GetAvailableAdapters();

        using var form = new SettingsForm(adapters, _settings);

        if (form.ShowDialog() != DialogResult.OK)
            return;

        _settings.AdapterId = form.SelectedAdapterId;
        _settings.PollingIntervalMs = form.PollingIntervalMs;
        _settings.SendThresholdBytesPerSecond = form.SendThresholdBytesPerSecond;
        _settings.ReceiveThresholdBytesPerSecond = form.ReceiveThresholdBytesPerSecond;
        _settings.StartWithWindows = form.StartWithWindows;
        _settings.EnableVerboseLogging = form.EnableVerboseLogging;

        AppLog.VerboseEnabled = _settings.EnableVerboseLogging;
        _settings.Save();

        _timer.Interval = Math.Clamp(_settings.PollingIntervalMs, 100, 2000);
        _monitor.ResetBaseline();

        try
        {
            StartupManager.SetEnabled(_settings.StartWithWindows);
        }
        catch (Exception ex)
        {
            AppLog.Error("Startup setting change failed", ex);
            MessageBox.Show(
                $"The startup setting could not be changed.\n\n{ex.Message}",
                "Network Activity Tray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            _settings.StartWithWindows = StartupManager.IsEnabled();
            _settings.Save();
        }

        _lastState = (ActivityState)(-1);
        OnTick();
    }

    private static void OpenLogFolder()
    {
        Directory.CreateDirectory(AppLog.LogDirectory);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppLog.LogDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not open log folder", ex);
        }
    }

    private void SyncStartupSetting()
    {
        bool actual = StartupManager.IsEnabled();
        if (_settings.StartWithWindows != actual)
        {
            _settings.StartWithWindows = actual;
            _settings.Save();
        }
    }

    private static string StateText(ActivityState state) => state switch
    {
        ActivityState.Both => "Sending + receiving",
        ActivityState.Send => "Sending",
        ActivityState.Receive => "Receiving",
        ActivityState.Idle => "Idle",
        ActivityState.Disconnected => "Disconnected",
        _ => state.ToString()
    };

    private static string TruncateTooltip(string value) =>
        value.Length <= 63 ? value : value[..63];

    private void ExitApplication()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        foreach (var icon in _icons.Values)
            icon.Dispose();

        _penmIcons.Dispose();
        _timer.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            foreach (var icon in _icons.Values)
                icon.Dispose();

            _penmIcons.Dispose();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
