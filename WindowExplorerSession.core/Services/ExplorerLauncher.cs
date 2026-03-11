using System.Diagnostics;

namespace WindowExplorerSession.Core;

internal static class ExplorerLauncher
{
    public static void OpenAddress(string address, bool separateProcess = false)
    {
        var escapedAddress = address.Replace("\"", "\"\"");
        var arguments = separateProcess
            ? $"/n,/separate,\"{escapedAddress}\""
            : $"/n,\"{escapedAddress}\"";

        var startInfo = new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true,
            Arguments = arguments
        };

        Process.Start(startInfo);
    }
}

