namespace WindowExplorerSession.Core;

internal static class VirtualDesktopNavigator
{
    public static bool TrySwitchToDesktopName(string? desktopName)
    {
        if (string.IsNullOrWhiteSpace(desktopName))
        {
            return true;
        }

        var targetDesktopId = VirtualDesktopRegistry.TryFindDesktopIdByName(desktopName);
        if (targetDesktopId is null || targetDesktopId == Guid.Empty)
        {
            return false;
        }

        return TrySwitchToDesktopId(targetDesktopId.Value);
    }

    public static bool TrySwitchToDesktopId(Guid targetDesktopId)
    {
        if (targetDesktopId == Guid.Empty)
        {
            return false;
        }

        var desktopOrder = VirtualDesktopRegistry.GetDesktopOrder();
        if (desktopOrder.Count == 0)
        {
            return false;
        }

        var targetIndex = IndexOfDesktop(desktopOrder, targetDesktopId);
        if (targetIndex < 0)
        {
            return false;
        }

        for (var attempt = 0; attempt < 40; attempt++)
        {
            var currentDesktopId = VirtualDesktopRegistry.TryGetCurrentDesktopId();
            var currentIndex = currentDesktopId.HasValue
                ? IndexOfDesktop(desktopOrder, currentDesktopId.Value)
                : -1;

            if (currentIndex == targetIndex)
            {
                return WaitForStableDesktop(targetDesktopId, TimeSpan.FromMilliseconds(800));
            }

            // If current desktop couldn't be resolved, nudge right once and try again.
            if (currentIndex < 0)
            {
                SendDesktopSwitchShortcut(moveRight: true);
                Thread.Sleep(135);
                continue;
            }

            SendDesktopSwitchShortcut(moveRight: currentIndex < targetIndex);
            Thread.Sleep(135);
        }

        return WaitForStableDesktop(targetDesktopId, TimeSpan.FromMilliseconds(900));
    }

    private static bool WaitForStableDesktop(Guid targetDesktopId, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var consecutiveMatches = 0;

        while (sw.Elapsed < timeout)
        {
            var currentDesktopId = VirtualDesktopRegistry.TryGetCurrentDesktopId();
            if (currentDesktopId.HasValue && currentDesktopId.Value == targetDesktopId)
            {
                consecutiveMatches++;
                if (consecutiveMatches >= 2)
                {
                    return true;
                }
            }
            else
            {
                consecutiveMatches = 0;
            }

            Thread.Sleep(70);
        }

        return false;
    }

    private static int IndexOfDesktop(IReadOnlyList<Guid> desktopOrder, Guid desktopId)
    {
        for (var i = 0; i < desktopOrder.Count; i++)
        {
            if (desktopOrder[i] == desktopId)
            {
                return i;
            }
        }

        return -1;
    }

    private static void SendDesktopSwitchShortcut(bool moveRight)
    {
        var direction = moveRight ? NativeMethods.VK_RIGHT : NativeMethods.VK_LEFT;

        NativeMethods.keybd_event(NativeMethods.VK_LWIN, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(direction, 0, 0, UIntPtr.Zero);

        NativeMethods.keybd_event(direction, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_LWIN, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
