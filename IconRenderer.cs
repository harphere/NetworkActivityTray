using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace NetworkActivityTray;

internal static class IconRenderer
{
    public static Icon Create(ActivityState state, int size = 32)
    {
        using var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        float s = size / 32f;
        PointF[] triangle =
        [
            new(16 * s, 4 * s),
            new(5 * s, 25 * s),
            new(27 * s, 25 * s)
        ];

        using var triangleFill = new SolidBrush(Color.White);
        using var outline = new Pen(Color.FromArgb(210, 35, 35, 35), Math.Max(1f, 1.4f * s));

        g.FillPolygon(triangleFill, triangle);
        g.DrawPolygon(outline, triangle);

        if (state != ActivityState.Disconnected)
            DrawActivityOverlay(g, state, s);
        else
            DrawDisconnectedOverlay(g, s);

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using Icon temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static void DrawActivityOverlay(Graphics g, ActivityState state, float s)
    {
        using var blue = new Pen(Color.FromArgb(255, 40, 145, 245), Math.Max(1.8f, 2.2f * s))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        if (state is ActivityState.Send or ActivityState.Both)
            DrawArrow(g, blue, new PointF(20.5f * s, 21f * s), new PointF(20.5f * s, 11.5f * s), true, s);

        if (state is ActivityState.Receive or ActivityState.Both)
            DrawArrow(g, blue, new PointF(11.5f * s, 11.5f * s), new PointF(11.5f * s, 21f * s), false, s);
    }

    private static void DrawArrow(Graphics g, Pen pen, PointF start, PointF end, bool up, float s)
    {
        g.DrawLine(pen, start, end);
        float x = end.X;
        float y = end.Y;
        float wing = 2.8f * s;

        if (up)
        {
            g.DrawLine(pen, new PointF(x, y), new PointF(x - wing, y + wing));
            g.DrawLine(pen, new PointF(x, y), new PointF(x + wing, y + wing));
        }
        else
        {
            g.DrawLine(pen, new PointF(x, y), new PointF(x - wing, y - wing));
            g.DrawLine(pen, new PointF(x, y), new PointF(x + wing, y - wing));
        }
    }

    private static void DrawDisconnectedOverlay(Graphics g, float s)
    {
        using var red = new Pen(Color.FromArgb(235, 220, 45, 45), Math.Max(2f, 2.4f * s))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLine(red, 10 * s, 11 * s, 22 * s, 22 * s);
        g.DrawLine(red, 22 * s, 11 * s, 10 * s, 22 * s);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
