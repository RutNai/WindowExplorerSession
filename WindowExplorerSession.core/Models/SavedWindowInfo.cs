namespace WindowExplorerSession.Core;

public sealed class SavedWindowInfo
{
    public int Index { get; init; }
    public required string Address { get; init; }
    public string? VirtualDesktopName { get; init; }
    public string? MonitorDeviceName { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}
