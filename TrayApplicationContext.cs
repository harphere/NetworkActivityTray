namespace NetworkActivityTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NetworkMonitor _monitor = new();
    private readonly Dictionary<ActivityState, Icon> _icons;
    private ActivityState _lastState = (ActivityState)(-1);

    public TrayApplicationContext()
    {
        _icons = Enum.GetValues<ActivityState>()
            .ToDictionary(state => state, state => IconRenderer.Create(state));

        var menu = new ContextMenuStrip();
        menu.Items.Add("Network Activity Tray v0.1").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());

        var adapterItem = new ToolStripMenuItem("Adapter: detecting…") { Enabled = false };
        menu.Items.Add(adapterItem);
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

        _timer = new System.Windows.Forms.Timer { Interval = 350 };
        _timer.Tick += (_, _) =>
        {
            var state = _monitor.Sample();
            adapterItem.Text = $"Adapter: {_monitor.AdapterName}";

            if (state != _lastState)
            {
                _notifyIcon.Icon = _icons[state];
                _lastState = state;
            }

            _notifyIcon.Text = TruncateTooltip($"Network Activity Tray\n{_monitor.AdapterName}\n{StateText(state)}");
        };
        _timer.Start();
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

    private static string TruncateTooltip(string value) => value.Length <= 63 ? value : value[..63];

    private void ExitApplication()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        foreach (var icon in _icons.Values) icon.Dispose();
        _timer.Dispose();
        ExitThread();
    }
}
