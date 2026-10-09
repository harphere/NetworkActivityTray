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

internal sealed class NetworkMonitor
{
    private NetworkInterface? _adapter;
    private long _lastSent;
    private long _lastReceived;
    private bool _hasBaseline;

    public string AdapterName => _adapter?.Name ?? "No active adapter";

    public ActivityState Sample()
    {
        var current = PickBestAdapter();

        if (current is null)
        {
            Reset();
            return ActivityState.Disconnected;
        }

        if (_adapter is null || !string.Equals(_adapter.Id, current.Id, StringComparison.OrdinalIgnoreCase))
        {
            _adapter = current;
            _hasBaseline = false;
        }

        IPv4InterfaceStatistics stats;
        try
        {
            stats = _adapter.GetIPv4Statistics();
        }
        catch
        {
            Reset();
            return ActivityState.Disconnected;
        }

        long sent = stats.BytesSent;
        long received = stats.BytesReceived;

        if (!_hasBaseline)
        {
            _lastSent = sent;
            _lastReceived = received;
            _hasBaseline = true;
            return ActivityState.Idle;
        }

        bool tx = sent > _lastSent;
        bool rx = received > _lastReceived;

        _lastSent = sent;
        _lastReceived = received;

        return (tx, rx) switch
        {
            (true, true) => ActivityState.Both,
            (true, false) => ActivityState.Send,
            (false, true) => ActivityState.Receive,
            _ => ActivityState.Idle
        };
    }

    private static NetworkInterface? PickBestAdapter()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                !n.Name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
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

    private void Reset()
    {
        _adapter = null;
        _hasBaseline = false;
        _lastSent = 0;
        _lastReceived = 0;
    }
}
