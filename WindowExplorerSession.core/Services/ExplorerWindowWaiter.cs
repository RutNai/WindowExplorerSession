using System.Diagnostics;

namespace WindowExplorerSession.Core;

internal static class ExplorerWindowWaiter
{
    public static IntPtr WaitForNewWindow(HashSet<IntPtr> existingHandles, string expectedAddress, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            foreach (var window in ExplorerWindowEnumerator.GetWindows())
            {
                if (existingHandles.Contains(window.Hwnd))
                {
                    continue;
                }

                if (AddressParser.Matches(window.Address, expectedAddress))
                {
                    return window.Hwnd;
                }
            }

            Thread.Sleep(150);
        }

        return IntPtr.Zero;
    }
}

