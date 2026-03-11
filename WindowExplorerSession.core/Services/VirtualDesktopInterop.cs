using System.Runtime.InteropServices;

namespace WindowExplorerSession.Core;

internal static class VirtualDesktopInterop
{
    public static Guid? TryGetWindowDesktopId(IntPtr hwnd)
    {
        try
        {
            var manager = GetManager();
            var hr = manager.GetWindowDesktopId(hwnd, out var desktopId);
            if (hr != 0 || desktopId == Guid.Empty)
            {
                return null;
            }

            return desktopId;
        }
        catch
        {
            return null;
        }
    }

    public static bool TryMoveWindowToDesktop(IntPtr hwnd, Guid? desktopId)
    {
        if (desktopId is null || desktopId == Guid.Empty)
        {
            return false;
        }

        try
        {
            var manager = GetManager();
            var hr = manager.MoveWindowToDesktop(hwnd, desktopId.Value);
            return hr == 0;
        }
        catch
        {
            // Ignore failures, for example when the desktop id no longer exists.
            return false;
        }
    }

    private static IVirtualDesktopManager GetManager()
    {
        return (IVirtualDesktopManager)new VirtualDesktopManagerCom();
    }

    [ComImport]
    [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktopManager
    {
        int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out bool onCurrentDesktop);
        int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);
        int MoveWindowToDesktop(IntPtr topLevelWindow, [In] Guid desktopId);
    }

    [ComImport]
    [Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")]
    private class VirtualDesktopManagerCom;
}

