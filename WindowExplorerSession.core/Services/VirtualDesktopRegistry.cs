using System.Text;
using Microsoft.Win32;

namespace WindowExplorerSession.Core;

internal static class VirtualDesktopRegistry
{
    private const string VirtualDesktopsContainer = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops";
    private const string VirtualDesktopsRoot = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops\Desktops";

    public static string? TryGetDesktopName(Guid? desktopId)
    {
        if (desktopId is null || desktopId == Guid.Empty)
        {
            return null;
        }

        using var key = Registry.CurrentUser.OpenSubKey($"{VirtualDesktopsRoot}\\{{{desktopId.Value}}}");
        return ReadDesktopName(key);
    }

    public static Guid? TryFindDesktopIdByName(string? desktopName)
    {
        if (string.IsNullOrWhiteSpace(desktopName))
        {
            return null;
        }

        var normalizedTarget = NormalizeName(desktopName);
        using var desktopsRoot = Registry.CurrentUser.OpenSubKey(VirtualDesktopsRoot);
        if (desktopsRoot is null)
        {
            return null;
        }

        foreach (var subKeyName in desktopsRoot.GetSubKeyNames())
        {
            if (!Guid.TryParse(subKeyName, out var desktopId))
            {
                continue;
            }

            using var desktopKey = desktopsRoot.OpenSubKey(subKeyName);
            var name = ReadDesktopName(desktopKey);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (string.Equals(NormalizeName(name), normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                return desktopId;
            }
        }

        return null;
    }

    public static IReadOnlyList<Guid> GetDesktopOrder()
    {
        using var root = Registry.CurrentUser.OpenSubKey(VirtualDesktopsContainer);
        var blob = root?.GetValue("VirtualDesktopIDs") as byte[];
        if (blob is null || blob.Length < 16)
        {
            return Array.Empty<Guid>();
        }

        var result = new List<Guid>(blob.Length / 16);
        for (var offset = 0; offset + 16 <= blob.Length; offset += 16)
        {
            var guidBytes = new byte[16];
            Buffer.BlockCopy(blob, offset, guidBytes, 0, 16);
            result.Add(new Guid(guidBytes));
        }

        return result;
    }

    public static Guid? TryGetCurrentDesktopId()
    {
        using var root = Registry.CurrentUser.OpenSubKey(VirtualDesktopsContainer);
        var raw = root?.GetValue("CurrentVirtualDesktop");
        return raw switch
        {
            byte[] bytes when bytes.Length >= 16 => new Guid(bytes.Take(16).ToArray()),
            string text when Guid.TryParse(text.Trim('{', '}'), out var guid) => guid,
            _ => null
        };
    }

    private static string? ReadDesktopName(RegistryKey? key)
    {
        if (key is null)
        {
            return null;
        }

        foreach (var valueName in new[] { "Name", "DesktopName", "VirtualDesktopName" })
        {
            var value = key.GetValue(valueName);
            var text = ConvertDesktopNameValue(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string? ConvertDesktopNameValue(object? value)
    {
        return value switch
        {
            string s => NormalizeName(s),
            byte[] bytes => DecodeUnicodeString(bytes),
            _ => null
        };
    }

    private static string? DecodeUnicodeString(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return null;
        }

        var validLength = bytes.Length - (bytes.Length % 2);
        var text = Encoding.Unicode.GetString(bytes, 0, validLength);
        return NormalizeName(text);
    }

    private static string? NormalizeName(string? value)
    {
        var normalized = value?.Trim().TrimEnd('\0');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

