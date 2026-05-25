using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TecmoScoreGrabber.Services;

/// <summary>
/// Captures a full monitor using Win32 <see cref="MONITORINFO.rcMonitor"/> so size/origin match
/// physical pixels on high-DPI / multi-monitor layouts (avoids WinForms <see cref="Screen.Bounds"/> + GDI mismatch).
/// For best results with GDI, use Windows display scaling at 100% on the captured monitor.
/// </summary>
public sealed class ScreenCaptureService
{
    private const uint MonitorDefaultToNearest = 2;

    public Bitmap CaptureMonitor(int monitorIndex)
    {
        var screens = Screen.AllScreens;
        if (monitorIndex < 0 || monitorIndex >= screens.Length)
            monitorIndex = 0;

        var screen = screens[monitorIndex];
        var b = screen.Bounds;

        var (x, y, w, h) = TryGetPhysicalMonitorRect(b, out var ok);
        if (!ok || w < 1 || h < 1)
        {
            x = b.Left;
            y = b.Top;
            w = Math.Max(1, b.Width);
            h = Math.Max(1, b.Height);
        }

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
        return bmp;
    }

    private static (int x, int y, int w, int h) TryGetPhysicalMonitorRect(Rectangle winFormsBounds, out bool ok)
    {
        ok = false;
        var cx = winFormsBounds.Left + Math.Max(1, winFormsBounds.Width) / 2;
        var cy = winFormsBounds.Top + Math.Max(1, winFormsBounds.Height) / 2;
        var hMonitor = MonitorFromPoint(new POINT { X = cx, Y = cy }, MonitorDefaultToNearest);
        if (hMonitor == IntPtr.Zero)
            return default;

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMonitor, ref mi))
            return default;

        var w = mi.rcMonitor.Right - mi.rcMonitor.Left;
        var h = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
        if (w < 1 || h < 1)
            return default;

        ok = true;
        return (mi.rcMonitor.Left, mi.rcMonitor.Top, w, h);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}
