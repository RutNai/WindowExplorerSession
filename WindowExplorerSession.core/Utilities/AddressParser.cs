namespace WindowExplorerSession.Core;

internal static class AddressParser
{
    public static string? ToAddress(string? locationUrl)
    {
        if (string.IsNullOrWhiteSpace(locationUrl))
        {
            return null;
        }

        if (Uri.TryCreate(locationUrl, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return locationUrl;
    }

    public static bool Matches(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeForKey(string pathOrUrl)
    {
        return Normalize(pathOrUrl);
    }

    private static string Normalize(string pathOrUrl)
    {
        return pathOrUrl.Trim().TrimEnd('\\', '/');
    }
}

