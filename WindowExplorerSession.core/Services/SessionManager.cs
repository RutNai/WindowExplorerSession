using System.Text.Json;

namespace WindowExplorerSession.Core;

public sealed class SessionManager
{
    private static int _restoreInProgress;
    private static readonly TimeSpan DesktopSwitchSettleDelay = TimeSpan.FromMilliseconds(0);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string SaveSession(string? filePath = null)
    {
        var effectivePath = string.IsNullOrWhiteSpace(filePath)
            ? SessionPathProvider.GetDefaultSaveSessionPath()
            : filePath;

        var windows = ExplorerWindowEnumerator.GetWindows().ToList();
        var session = new ExplorerSession
        {
            CapturedAtUtc = DateTime.UtcNow,
            Windows = windows
        };

        Directory.CreateDirectory(Path.GetDirectoryName(effectivePath) ?? SessionPathProvider.GetDefaultSessionDirectory());
        File.WriteAllText(effectivePath, JsonSerializer.Serialize(session, JsonOptions));

        return effectivePath;
    }

    public int RestoreSession(string? filePath = null)
    {
        var startingDesktopId = VirtualDesktopRegistry.TryGetCurrentDesktopId();
        var restoredHandles = new HashSet<IntPtr>();

        if (Interlocked.CompareExchange(ref _restoreInProgress, 1, 0) != 0)
        {
            throw new InvalidOperationException("Restore is already in progress.");
        }

        try
        {
            var effectivePath = string.IsNullOrWhiteSpace(filePath)
                ? SessionPathProvider.GetDefaultLatestSessionPath()
                : filePath;

            if (string.IsNullOrWhiteSpace(effectivePath))
            {
                throw new FileNotFoundException($"No session files found in '{SessionPathProvider.GetDefaultSessionDirectory()}'.");
            }

            if (!File.Exists(effectivePath))
            {
                throw new FileNotFoundException($"Session file not found: '{effectivePath}'", effectivePath);
            }

            var session = JsonSerializer.Deserialize<ExplorerSession>(File.ReadAllText(effectivePath), JsonOptions);
            if (session?.Windows is null || session.Windows.Count == 0)
            {
                throw new InvalidDataException("Session file has no Explorer windows to restore.");
            }

            var existingWindows = ExplorerWindowEnumerator.GetWindows().ToList();
            var existingHandles = existingWindows.Select(w => w.Hwnd).ToHashSet();

            var restored = RestoreSessionDesktopBlocking(
                OrderByDesktop(session.Windows),
                existingHandles,
                restoredHandles);

            return restored;
        }
        finally
        {
            if (startingDesktopId.HasValue)
            {
                _ = VirtualDesktopNavigator.TrySwitchToDesktopId(startingDesktopId.Value);
            }

            foreach (var hwnd in restoredHandles)
            {
                WindowRestorer.StopTaskbarBlink(hwnd);
            }

            // Some Explorer windows signal attention after delayed initialization.
            Thread.Sleep(220);
            foreach (var hwnd in restoredHandles)
            {
                WindowRestorer.StopTaskbarBlink(hwnd);
            }

            Interlocked.Exchange(ref _restoreInProgress, 0);
        }
    }

    public int RestoreSessionWindow(string filePath, int windowIndex)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Session file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Session file not found: '{filePath}'", filePath);
        }

        var session = JsonSerializer.Deserialize<ExplorerSession>(File.ReadAllText(filePath), JsonOptions);
        if (session?.Windows is null || session.Windows.Count == 0)
        {
            throw new InvalidDataException("Session file has no Explorer windows to restore.");
        }

        if (windowIndex < 0 || windowIndex >= session.Windows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(windowIndex), "Window index is out of range.");
        }

        var existingWindows = ExplorerWindowEnumerator.GetWindows().ToList();
        var existingHandles = existingWindows.Select(w => w.Hwnd).ToHashSet();
        var alreadyOpenHandleQueues = OpenWindowTracker.BuildHandleQueues(existingWindows);

        if (Interlocked.CompareExchange(ref _restoreInProgress, 1, 0) != 0)
        {
            throw new InvalidOperationException("Restore is already in progress.");
        }

        try
        {
            var state = session.Windows[windowIndex];
            _ = VirtualDesktopNavigator.TrySwitchToDesktopName(state.VirtualDesktopName);

            if (!TryRestoreWindowState(state, existingHandles, alreadyOpenHandleQueues, out var restoredHwnd))
            {
                return 0;
            }

            WindowRestorer.StopTaskbarBlink(restoredHwnd);
            return 1;
        }
        finally
        {
            Interlocked.Exchange(ref _restoreInProgress, 0);
        }
    }

    public IReadOnlyList<SavedWindowInfo> GetSessionWindows(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Session file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Session file not found: '{filePath}'", filePath);
        }

        var session = JsonSerializer.Deserialize<ExplorerSession>(File.ReadAllText(filePath), JsonOptions);
        if (session?.Windows is null || session.Windows.Count == 0)
        {
            return Array.Empty<SavedWindowInfo>();
        }

        return session.Windows
            .Select((w, idx) => new SavedWindowInfo
            {
                Index = idx,
                Address = w.Address,
                VirtualDesktopName = w.VirtualDesktopName,
                MonitorDeviceName = w.MonitorDeviceName,
                X = w.X,
                Y = w.Y,
                Width = w.Width,
                Height = w.Height
            })
            .ToList();
    }

    public int DeleteSessionWindow(string filePath, int windowIndex)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Session file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Session file not found: '{filePath}'", filePath);
        }

        var session = JsonSerializer.Deserialize<ExplorerSession>(File.ReadAllText(filePath), JsonOptions);
        if (session?.Windows is null || session.Windows.Count == 0)
        {
            return 0;
        }

        if (windowIndex < 0 || windowIndex >= session.Windows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(windowIndex), "Window index is out of range.");
        }

        session.Windows.RemoveAt(windowIndex);
        File.WriteAllText(filePath, JsonSerializer.Serialize(session, JsonOptions));
        return session.Windows.Count;
    }

    public IReadOnlyList<SessionFileInfo> ListSessions()
    {
        var directory = SessionPathProvider.GetDefaultSessionDirectory();
        if (!Directory.Exists(directory))
        {
            return Array.Empty<SessionFileInfo>();
        }

        var files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        var results = new List<SessionFileInfo>(files.Count);
        foreach (var file in files)
        {
            var capturedAtUtc = default(DateTime?);
            var windowCount = 0;

            try
            {
                var session = JsonSerializer.Deserialize<ExplorerSession>(File.ReadAllText(file), JsonOptions);
                capturedAtUtc = session?.CapturedAtUtc;
                windowCount = session?.Windows?.Count ?? 0;
            }
            catch
            {
                // Keep malformed files visible in list for troubleshooting.
            }

            results.Add(new SessionFileInfo
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                LastWriteTimeUtc = File.GetLastWriteTimeUtc(file),
                CapturedAtUtc = capturedAtUtc,
                WindowCount = windowCount
            });
        }

        return results;
    }

    public void DeleteSession(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Session file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Session file not found: '{filePath}'", filePath);
        }

        File.Delete(filePath);
    }

    public string GetDefaultSessionDirectory()
    {
        return SessionPathProvider.GetDefaultSessionDirectory();
    }

    public string? GetLatestSessionPath()
    {
        return SessionPathProvider.GetDefaultLatestSessionPath();
    }

    private static bool TryRestoreWindowState(
        ExplorerWindowState state,
        HashSet<IntPtr> existingHandles,
        Dictionary<string, Queue<IntPtr>> alreadyOpenHandleQueues,
        out IntPtr restoredHwnd)
    {
        restoredHwnd = IntPtr.Zero;

        if (string.IsNullOrWhiteSpace(state.Address))
        {
            return false;
        }

        if (OpenWindowTracker.TryTakeAlreadyOpenWindowHandle(alreadyOpenHandleQueues, state.Address, out var openHwnd))
        {
            WindowRestorer.Restore(openHwnd, state);
            restoredHwnd = openHwnd;
            return true;
        }

        ExplorerLauncher.OpenAddress(state.Address);
        var hwnd = ExplorerWindowWaiter.WaitForNewWindow(existingHandles, state.Address, TimeSpan.FromSeconds(8));
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        existingHandles.Add(hwnd);
        WindowRestorer.Restore(hwnd, state);
        restoredHwnd = hwnd;
        return true;
    }

    private static int RestoreSessionDesktopBlocking(
        IReadOnlyList<DesktopRestoreGroup> desktopGroups,
        HashSet<IntPtr> existingHandles,
        ISet<IntPtr> restoredHandles)
    {
        var restored = 0;

        foreach (var group in desktopGroups)
        {
            EnsureDesktopReady(group.DesktopName);

            var restoredInGroup = new List<(IntPtr Hwnd, ExplorerWindowState State)>();

            foreach (var state in group.Windows)
            {
                if (string.IsNullOrWhiteSpace(state.Address))
                {
                    continue;
                }

                EnsureDesktopReady(group.DesktopName);
                ExplorerLauncher.OpenAddress(state.Address, separateProcess: false);
                var matchedHwnd = ExplorerWindowWaiter.WaitForNewWindow(existingHandles, state.Address, TimeSpan.FromSeconds(12));
                if (matchedHwnd != IntPtr.Zero)
                {
                    existingHandles.Add(matchedHwnd);
                    WindowRestorer.Restore(matchedHwnd, state);
                    WindowRestorer.StopTaskbarBlink(matchedHwnd);
                    _ = restoredHandles.Add(matchedHwnd);
                    restoredInGroup.Add((matchedHwnd, state));
                    restored++;
                }
            }

            // Explorer can resize after initial creation; apply saved placement again once windows settle.
            if (restoredInGroup.Count > 0)
            {
                Thread.Sleep(260);
                foreach (var restoredItem in restoredInGroup)
                {
                    WindowRestorer.Restore(restoredItem.Hwnd, restoredItem.State);
                    WindowRestorer.StopTaskbarBlink(restoredItem.Hwnd);
                }
            }

            // Give shell state a moment to settle before moving to the next virtual desktop.
            Thread.Sleep(DesktopSwitchSettleDelay);
        }

        return restored;
    }

    private static void EnsureDesktopReady(string? desktopName)
    {
        if (VirtualDesktopNavigator.TrySwitchToDesktopName(desktopName))
        {
            return;
        }

        // Fallback settle delay if Windows did not confirm desktop switch.
        Thread.Sleep(180);
    }

    private static IReadOnlyList<DesktopRestoreGroup> OrderByDesktop(IReadOnlyList<ExplorerWindowState> windows)
    {
        var order = VirtualDesktopRegistry.GetDesktopOrder();
        var nameById = order
            .Select(id => new { Id = id, Name = VirtualDesktopRegistry.TryGetDesktopName(id) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select((x, idx) => new { x.Name, Index = idx })
            .ToDictionary(x => x.Name!, x => x.Index, StringComparer.OrdinalIgnoreCase);

        var grouped = windows
            .GroupBy(w => string.IsNullOrWhiteSpace(w.VirtualDesktopName) ? string.Empty : w.VirtualDesktopName!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DesktopRestoreGroup
            {
                DesktopName = string.IsNullOrWhiteSpace(g.Key) ? null : g.Key,
                Windows = g.ToList()
            })
            .ToList();

        return grouped
            .OrderBy(g => g.DesktopName is null ? int.MaxValue : nameById.GetValueOrDefault(g.DesktopName, int.MaxValue - 1))
            .ThenBy(g => g.DesktopName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class DesktopRestoreGroup
    {
        public string? DesktopName { get; init; }
        public required List<ExplorerWindowState> Windows { get; init; }
    }

}
