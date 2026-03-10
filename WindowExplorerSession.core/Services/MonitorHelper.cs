using System.Runtime.InteropServices;

namespace WindowExplorerSession.Core;

internal static class MonitorHelper
{
    public static MonitorCapture GetMonitor(IntPtr hwnd)
    {
        var monitorHandle = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitorHandle == IntPtr.Zero)
        {
            return new MonitorCapture();
        }

        var monitorInfo = new NativeMethods.MONITORINFOEX();
        monitorInfo.cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>();
        if (!NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo))
        {
            return new MonitorCapture();
        }

        NativeMethods.GetWindowRect(hwnd, out var rect);
        return new MonitorCapture
        {
            DeviceName = monitorInfo.szDevice,
            RelativeX = rect.Left - monitorInfo.rcMonitor.Left,
            RelativeY = rect.Top - monitorInfo.rcMonitor.Top
        };
    }

    public static RectangleState GetTargetRect(ExplorerWindowState state)
    {
        if (!string.IsNullOrWhiteSpace(state.MonitorDeviceName))
        {
            foreach (var monitor in NativeMethods.GetAllMonitors())
            {
                if (string.Equals(monitor.DeviceName, state.MonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return new RectangleState
                    {
                        X = monitor.Left + state.MonitorRelativeX,
                        Y = monitor.Top + state.MonitorRelativeY,
                        Width = state.Width,
                        Height = state.Height
                    };
                }
            }
        }

        return new RectangleState
        {
            X = state.X,
            Y = state.Y,
            Width = state.Width,
            Height = state.Height
        };
    }
}

