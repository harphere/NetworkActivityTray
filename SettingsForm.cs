namespace NetworkActivityTray;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox _adapterCombo = new();
    private readonly NumericUpDown _polling = new();
    private readonly NumericUpDown _sendThreshold = new();
    private readonly NumericUpDown _receiveThreshold = new();
    private readonly CheckBox _startup = new();
    private readonly CheckBox _verboseLogging = new();

    public string? SelectedAdapterId { get; private set; }
    public int PollingIntervalMs { get; private set; }
    public long SendThresholdBytesPerSecond { get; private set; }
    public long ReceiveThresholdBytesPerSecond { get; private set; }
    public bool StartWithWindows { get; private set; }
    public bool EnableVerboseLogging { get; private set; }

    public SettingsForm(IReadOnlyList<AdapterInfo> adapters, AppSettings settings)
    {
        Text = "Network Activity Tray Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(590, 330);

        int labelX = 18;
        int fieldX = 190;

        Controls.Add(new Label { Text = "Network adapter:", AutoSize = true, Location = new Point(labelX, 26) });
        _adapterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _adapterCombo.Location = new Point(fieldX, 21);
        _adapterCombo.Size = new Size(375, 28);
        _adapterCombo.Items.Add(new AdapterChoice(null, "Automatic"));
        foreach (var adapter in adapters)
            _adapterCombo.Items.Add(new AdapterChoice(adapter.Id, adapter.ToString()));

        int selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(settings.AdapterId))
        {
            for (int i = 0; i < _adapterCombo.Items.Count; i++)
            {
                if (_adapterCombo.Items[i] is AdapterChoice c &&
                    string.Equals(c.Id, settings.AdapterId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        _adapterCombo.SelectedIndex = selectedIndex;

        Controls.Add(new Label { Text = "Polling interval:", AutoSize = true, Location = new Point(labelX, 77) });
        ConfigureNumeric(_polling, fieldX, 72, 100, 2000, 50, settings.PollingIntervalMs);
        Controls.Add(new Label { Text = "ms", AutoSize = true, Location = new Point(fieldX + 120, 77) });

        Controls.Add(new Label { Text = "Send threshold:", AutoSize = true, Location = new Point(labelX, 128) });
        ConfigureNumeric(_sendThreshold, fieldX, 123, 0, 10_000_000, 512, settings.SendThresholdBytesPerSecond);
        Controls.Add(new Label { Text = "bytes/sec", AutoSize = true, Location = new Point(fieldX + 120, 128) });

        Controls.Add(new Label { Text = "Receive threshold:", AutoSize = true, Location = new Point(labelX, 179) });
        ConfigureNumeric(_receiveThreshold, fieldX, 174, 0, 10_000_000, 512, settings.ReceiveThresholdBytesPerSecond);
        Controls.Add(new Label { Text = "bytes/sec", AutoSize = true, Location = new Point(fieldX + 120, 179) });

        _startup.Text = "Start with Windows";
        _startup.AutoSize = true;
        _startup.Location = new Point(fieldX, 220);
        _startup.Checked = settings.StartWithWindows;

        _verboseLogging.Text = "Enable verbose diagnostic logging";
        _verboseLogging.AutoSize = true;
        _verboseLogging.Location = new Point(fieldX, 250);
        _verboseLogging.Checked = settings.EnableVerboseLogging;

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(400, 286), Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(490, 286), Width = 75 };

        AcceptButton = ok;
        CancelButton = cancel;

        Controls.AddRange([_adapterCombo, _polling, _sendThreshold, _receiveThreshold, _startup, _verboseLogging, ok, cancel]);
        ok.Click += (_, _) => SaveValues();
    }

    private static void ConfigureNumeric(
        NumericUpDown control,
        int x, int y,
        decimal min, decimal max, decimal increment, decimal value)
    {
        control.Location = new Point(x, y);
        control.Minimum = min;
        control.Maximum = max;
        control.Increment = increment;
        control.Value = Math.Clamp(value, min, max);
        control.Width = 110;
        control.ThousandsSeparator = true;
    }

    private void SaveValues()
    {
        var selected = _adapterCombo.SelectedItem as AdapterChoice;
        SelectedAdapterId = selected?.Id;
        PollingIntervalMs = (int)_polling.Value;
        SendThresholdBytesPerSecond = (long)_sendThreshold.Value;
        ReceiveThresholdBytesPerSecond = (long)_receiveThreshold.Value;
        StartWithWindows = _startup.Checked;
        EnableVerboseLogging = _verboseLogging.Checked;
    }

    private sealed record AdapterChoice(string? Id, string Display)
    {
        public override string ToString() => Display;
    }
}
