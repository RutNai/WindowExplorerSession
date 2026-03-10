namespace WindowExplorerSession.Core;

internal static class OpenWindowTracker
{
    public static Dictionary<string, Queue<IntPtr>> BuildHandleQueues(IEnumerable<ExplorerWindowState> windows)
    {
        var queues = new Dictionary<string, Queue<IntPtr>>(StringComparer.OrdinalIgnoreCase);
        foreach (var window in windows)
        {
            var address = window.Address;
            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            var key = AddressParser.NormalizeForKey(address);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!queues.TryGetValue(key, out var handleQueue))
            {
                handleQueue = new Queue<IntPtr>();
                queues[key] = handleQueue;
            }

            handleQueue.Enqueue(window.Hwnd);
        }

        return queues;
    }

    public static bool TryTakeAlreadyOpenWindowHandle(
        Dictionary<string, Queue<IntPtr>> openHandleQueues,
        string? address,
        out IntPtr hwnd)
    {
        hwnd = IntPtr.Zero;
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        var key = AddressParser.NormalizeForKey(address);
        if (!openHandleQueues.TryGetValue(key, out var handleQueue) || handleQueue.Count == 0)
        {
            return false;
        }

        hwnd = handleQueue.Dequeue();
        return true;
    }
}

