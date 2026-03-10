using System.Text.Json.Serialization;

namespace WindowExplorerSession.Core;

internal sealed class ExplorerWindowState
{
    [JsonIgnore]
    public IntPtr Hwnd { get; set; }

    public string Address { get; set; } = string.Empty;
    public string? LocationUrl { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? VirtualDesktopName { get; set; }

    [JsonPropertyName("VirtualDesktopId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? LegacyVirtualDesktopId { get; set; }

    public string? MonitorDeviceName { get; set; }
    public int MonitorRelativeX { get; set; }
    public int MonitorRelativeY { get; set; }
}

