using System.Diagnostics;
using System.Net.NetworkInformation;

namespace NetworkActivityTray;

internal enum ActivityState
{
    Disconnected,
    Idle,
    Send,
    Receive,
    Both
}

internal sealed class AdapterInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required OperationalStatus Status { get; init; }
    public required NetworkInterfaceType Type { get; init; }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} — {Description}";
}

internal sealed class NetworkMonitor
{
    private readonly Func<string?> _adapterIdProvider;
    private readonly Func<long> _sendThresholdProvider;
    private readonly Func<long> _receiveThresholdProvider;

    private NetworkInterface? _adapter;
    private long _lastSent;
    private long _lastReceived;
    private long _lastTimestamp;
    private bool _hasBaseline;

    public NetworkMonitor(
        Func<string?> adapterIdProvider,
        Func<long> sendThresholdProvider,
        Func<long> receiveThresholdProvider)
    {
        _adapterIdProvider = adapterIdProvider;
        _sendThresholdProvider = sendThresholdProvider;
        _receiveThresholdProvider = receiveThresholdProvider;
    }

    public string AdapterName => _adapter?.Name ?? "No active adapter";
    public double LastSendBytesPerSecond { get; private set; }
    public double LastReceiveBytesPerSecond { get; private set; }

    public IReadOnlyList<AdapterInfo> GetAvailableAdapters()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(IsCandidateAdapter)
            .Select(n => new AdapterInfo
            {
                Id = n.Id,
                Name = n.Name,
                Description = n.Description,
                Status = n.OperationalStatus,
                Type = n.NetworkInterfaceType
            })
            .OrderByDescending(a => a.Status == OperationalStatus.Up)
            .ThenBy(a => a.Name)
            .ToList();
    }

    public ActivityState Sample()
    {
        var current = ResolveAdapter();

        if (current is null)
        {
            Reset();
            return ActivityState.Disconnected;
        }

        if (_adapter is null || !string.Equals(_adapter.Id, current.Id, StringComparison.OrdinalIgnoreCase))
        {
            _adapter = current;
            _hasBaseline = false;
            AppLog.Info($"Monitoring adapter: {_adapter.Name} ({_adapter.Description})");
        }

        IPv4InterfaceStatistics stats;
        try
        {
            stats = _adapter.GetIPv4Statistics();
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to read adapter statistics", ex);
            Reset();
            return ActivityState.Disconnected;
        }

        long sent = stats.BytesSent;
        long received = stats.BytesReceived;
        long now = Stopwatch.GetTimestamp();

        if (!_hasBaseline)
        {
            _lastSent = sent;
            _lastReceived = received;
            _lastTimestamp = now;
            _hasBaseline = true;
            return ActivityState.Idle;
        }

        double elapsedSeconds = (now - _lastTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds <= 0)
            elapsedSeconds = 0.001;

        long sentDelta = Math.Max(0, sent - _lastSent);
        long receiveDelta = Math.Max(0, received - _lastReceived);

        LastSendBytesPerSecond = sentDelta / elapsedSeconds;
        LastReceiveBytesPerSecond = receiveDelta / elapsedSeconds;

        bool tx = LastSendBytesPerSecond >= _sendThresholdProvider();
        bool rx = LastReceiveBytesPerSecond >= _receiveThresholdProvider();

        _lastSent = sent;
        _lastReceived = received;
        _lastTimestamp = now;

        return (tx, rx) switch
        {
            (true, true) => ActivityState.Both,
            (true, false) => ActivityState.Send,
            (false, true) => ActivityState.Receive,
            _ => ActivityState.Idle
        };
    }

    private NetworkInterface? ResolveAdapter()
    {
        string? preferredId = _adapterIdProvider();

        var adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(IsCandidateAdapter)
            .ToList();

        if (!string.IsNullOrWhiteSpace(preferredId))
        {
            var selected = adapters.FirstOrDefault(n =>
                string.Equals(n.Id, preferredId, StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
                return selected;
        }

        return adapters
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Select(n => new
            {
                Adapter = n,
                HasGateway = HasUsableGateway(n),
                Speed = n.Speed
            })
            .OrderByDescending(x => x.HasGateway)
            .ThenByDescending(x => x.Speed)
            .Select(x => x.Adapter)
            .FirstOrDefault();
    }

    private static bool IsCandidateAdapter(NetworkInterface n) =>
        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
        !n.Name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase);

    private static bool HasUsableGateway(NetworkInterface adapter)
    {
        try
        {
            return adapter.GetIPProperties()
                .GatewayAddresses
                .Any(g => g.Address is not null &&
                          !g.Address.Equals(System.Net.IPAddress.Any) &&
                          !g.Address.Equals(System.Net.IPAddress.IPv6Any));
        }
        catch
        {
            return false;
        }
    }

    public void ResetBaseline() => _hasBaseline = false;

    private void Reset()
    {
        _adapter = null;
        _hasBaseline = false;
        _lastSent = 0;
        _lastReceived = 0;
        _lastTimestamp = 0;
        LastSendBytesPerSecond = 0;
        LastReceiveBytesPerSecond = 0;
    }
}
