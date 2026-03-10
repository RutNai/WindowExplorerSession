namespace WindowExplorerSession.Core;

internal static class WindowRestorer
{
    public static void Restore(IntPtr hwnd, ExplorerWindowState state)
    {
        Guid? targetDesktopId = null;
        if (!string.IsNullOrWhiteSpace(state.VirtualDesktopName))
        {
            targetDesktopId = VirtualDesktopRegistry.TryFindDesktopIdByName(state.VirtualDesktopName);
        }

        // Backward compatibility for older session files that only stored desktop IDs.
        targetDesktopId ??= state.LegacyVirtualDesktopId;
        VirtualDesktopInterop.TryMoveWindowToDesktop(hwnd, targetDesktopId);

        var targetRect = MonitorHelper.GetTargetRect(state);
        NativeMethods.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            targetRect.X,
            targetRect.Y,
            targetRect.Width,
            targetRect.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
    }
}

