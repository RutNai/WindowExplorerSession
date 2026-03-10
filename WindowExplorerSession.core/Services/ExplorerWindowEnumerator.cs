using System.Runtime.InteropServices;

namespace WindowExplorerSession.Core;

internal static class ExplorerWindowEnumerator
{
    public static IEnumerable<ExplorerWindowState> GetWindows()
    {
        dynamic? shellApp = null;
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                yield break;
            }

            shellApp = Activator.CreateInstance(shellType);
            dynamic windows = shellApp!.Windows();
            var count = (int)windows.Count;

            for (var i = 0; i < count; i++)
            {
                dynamic? window = null;
                try
                {
                    window = windows.Item(i);
                    if (window is null)
                    {
                        continue;
                    }

                    var fullName = ((string?)window.FullName ?? string.Empty).ToLowerInvariant();
                    if (!fullName.EndsWith("explorer.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var hwnd = new IntPtr((int)window.HWND);
                    var locationUrl = (string?)window.LocationURL;
                    var address = AddressParser.ToAddress(locationUrl);
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        continue;
                    }

                    if (!NativeMethods.GetWindowRect(hwnd, out var rect))
                    {
                        continue;
                    }

                    var monitor = MonitorHelper.GetMonitor(hwnd);
                    var desktopId = VirtualDesktopInterop.TryGetWindowDesktopId(hwnd);
                    var desktopName = VirtualDesktopRegistry.TryGetDesktopName(desktopId);

                    yield return new ExplorerWindowState
                    {
                        Hwnd = hwnd,
                        Address = address,
                        LocationUrl = locationUrl,
                        X = rect.Left,
                        Y = rect.Top,
                        Width = Math.Max(100, rect.Right - rect.Left),
                        Height = Math.Max(100, rect.Bottom - rect.Top),
                        VirtualDesktopName = desktopName,
                        MonitorDeviceName = monitor.DeviceName,
                        MonitorRelativeX = monitor.RelativeX,
                        MonitorRelativeY = monitor.RelativeY
                    };
                }
                finally
                {
                    if (window is not null)
                    {
                        Marshal.ReleaseComObject(window);
                    }
                }
            }

            Marshal.ReleaseComObject(windows);
        }
        finally
        {
            if (shellApp is not null)
            {
                Marshal.ReleaseComObject(shellApp);
            }
        }
    }
}

