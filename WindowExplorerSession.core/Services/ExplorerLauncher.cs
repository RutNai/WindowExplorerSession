using System.Diagnostics;

namespace WindowExplorerSession.Core;

internal static class ExplorerLauncher
{
    public static void OpenAddress(string address)
    {
        var startInfo = new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true,
            Arguments = address
        };

        Process.Start(startInfo);
    }
}

