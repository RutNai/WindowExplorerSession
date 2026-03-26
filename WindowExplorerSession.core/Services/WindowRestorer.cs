namespace WindowExplorerSession.Core;

internal static class WindowRestorer
{
    public static void Restore(IntPtr hwnd, ExplorerWindowState state)
    {
        var targetDesktopId = VirtualDesktopRegistry.TryFindDesktopIdByName(state.VirtualDesktopName);
        EnsureWindowOnDesktop(hwnd, targetDesktopId);

        var targetRect = MonitorHelper.GetTargetRect(state);
        NativeMethods.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            targetRect.X,
            targetRect.Y,
            targetRect.Width,
            targetRect.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);

        // Explorer can re-home after first layout; verify once more after position restore.
        EnsureWindowOnDesktop(hwnd, targetDesktopId);
    }

    private static void EnsureWindowOnDesktop(IntPtr hwnd, Guid? targetDesktopId)
    {
        if (hwnd == IntPtr.Zero || targetDesktopId is null || targetDesktopId == Guid.Empty)
        {
            return;
        }

        var actualDesktopId = VirtualDesktopInterop.TryGetWindowDesktopId(hwnd);
        if (actualDesktopId.HasValue && actualDesktopId.Value == targetDesktopId.Value)
        {
            return;
        }

        for (var attempt = 0; attempt < 14; attempt++)
        {
            var moved = VirtualDesktopInterop.TryMoveWindowToDesktop(hwnd, targetDesktopId);
            if (!moved)
            {
                Thread.Sleep(30);
                continue;
            }

            actualDesktopId = VirtualDesktopInterop.TryGetWindowDesktopId(hwnd);
            if (actualDesktopId.HasValue && actualDesktopId.Value == targetDesktopId.Value)
            {
                return;
            }

            Thread.Sleep(30);
        }
    }

    public static void StopTaskbarBlink(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return;
        }

        var info = new NativeMethods.FLASHWINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.FLASHWINFO>(),
            hwnd = hwnd,
            dwFlags = NativeMethods.FLASHW_STOP,
            uCount = 0,
            dwTimeout = 0
        };

        _ = NativeMethods.FlashWindowEx(ref info);
    }
}

