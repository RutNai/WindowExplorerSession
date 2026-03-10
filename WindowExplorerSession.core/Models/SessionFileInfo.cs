namespace WindowExplorerSession.Core;

public sealed class SessionFileInfo
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public DateTime LastWriteTimeUtc { get; init; }
    public DateTime? CapturedAtUtc { get; init; }
    public int WindowCount { get; init; }
}
