namespace WindowExplorerSession.Core;

internal sealed class ExplorerSession
{
    public DateTime CapturedAtUtc { get; set; }
    public List<ExplorerWindowState> Windows { get; set; } = new();
}

