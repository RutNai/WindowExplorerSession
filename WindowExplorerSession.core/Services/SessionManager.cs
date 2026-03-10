using System.Text.Json;

namespace WindowExplorerSession.Core;

public sealed class SessionManager
{
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
        var alreadyOpenHandleQueues = OpenWindowTracker.BuildHandleQueues(existingWindows);

        var restored = 0;
        foreach (var state in session.Windows)
        {
            if (string.IsNullOrWhiteSpace(state.Address))
            {
                continue;
            }

            if (OpenWindowTracker.TryTakeAlreadyOpenWindowHandle(alreadyOpenHandleQueues, state.Address, out var openHwnd))
            {
                WindowRestorer.Restore(openHwnd, state);
                restored++;
                continue;
            }

            ExplorerLauncher.OpenAddress(state.Address);
            var hwnd = ExplorerWindowWaiter.WaitForNewWindow(existingHandles, state.Address, TimeSpan.FromSeconds(8));
            if (hwnd == IntPtr.Zero)
            {
                continue;
            }

            existingHandles.Add(hwnd);
            WindowRestorer.Restore(hwnd, state);
            restored++;
        }

        return restored;
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
}
