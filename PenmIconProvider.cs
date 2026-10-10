using System.Runtime.InteropServices;

namespace NetworkActivityTray;

internal sealed class PenmIconProvider : IDisposable
{
    private const int BaseTriangleResource = 100;
    private const int BothOverlayResource = 88;
    private const int SendOverlayResource = 89;
    private const int ReceiveOverlayResource = 90;
    private const int IdleOverlayResource = 91;
    private const int DisconnectedResource = 80;

    private readonly string _iconLibraryPath;
    private readonly Dictionary<ActivityState, Icon> _icons = new();

    public bool IsAvailable { get; private set; }

    public PenmIconProvider(string iconLibraryPath)
    {
        _iconLibraryPath = iconLibraryPath;
        IsAvailable = File.Exists(_iconLibraryPath);
        if (!IsAvailable) return;

        using var triangle = ExtractResourceIcon(BaseTriangleResource);
        using var both = ExtractResourceIcon(BothOverlayResource);
        using var send = ExtractResourceIcon(SendOverlayResource);
        using var receive = ExtractResourceIcon(ReceiveOverlayResource);
        using var idle = ExtractResourceIcon(IdleOverlayResource);
        using var disconnected = ExtractResourceIcon(DisconnectedResource);

        if (triangle is null)
        {
            IsAvailable = false;
            return;
        }

        _icons[ActivityState.Idle] = Compose(triangle, idle);
        _icons[ActivityState.Send] = Compose(triangle, send);
        _icons[ActivityState.Receive] = Compose(triangle, receive);
        _icons[ActivityState.Both] = Compose(triangle, both);
        _icons[ActivityState.Disconnected] =
            disconnected is not null ? (Icon)disconnected.Clone() : IconRenderer.Create(ActivityState.Disconnected);
    }

    public Icon? Get(ActivityState state) =>
        _icons.TryGetValue(state, out var icon) ? icon : null;

    private Icon? ExtractResourceIcon(int resourceId)
    {
        IntPtr[] large = new IntPtr[1];
        IntPtr[] small = new IntPtr[1];

        uint count = ExtractIconEx(_iconLibraryPath, -resourceId, large, small, 1);
        if (count == 0) return null;

        IntPtr chosen = small[0] != IntPtr.Zero ? small[0] : large[0];
        if (chosen == IntPtr.Zero) return null;

        try
        {
            using var temp = Icon.FromHandle(chosen);
            return (Icon)temp.Clone();
        }
        finally
        {
            if (large[0] != IntPtr.Zero) DestroyIcon(large[0]);
            if (small[0] != IntPtr.Zero && small[0] != large[0]) DestroyIcon(small[0]);
        }
    }

    private static Icon Compose(Icon baseIcon, Icon? overlayIcon)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.DrawIcon(baseIcon, new Rectangle(0, 0, size, size));
        if (overlayIcon is not null)
            g.DrawIcon(overlayIcon, new Rectangle(0, 0, size, size));

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally { DestroyIcon(hIcon); }
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values) icon.Dispose();
        _icons.Clear();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(
        string szFileName, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
