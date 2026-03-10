namespace WindowExplorerSession.Core;

internal static class SessionPathProvider
{
    private const string SessionFilePattern = "*.json";

    public static string GetDefaultSaveSessionPath()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return Path.Combine(GetDefaultSessionDirectory(), $"[{timestamp}]_session.json");
    }

    public static string? GetDefaultLatestSessionPath()
    {
        var sessionDirectory = GetDefaultSessionDirectory();
        if (!Directory.Exists(sessionDirectory))
        {
            return null;
        }

        return Directory.GetFiles(sessionDirectory, SessionFilePattern, SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public static string GetDefaultSessionDirectory()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "WindowExplorerSession");
    }
}

