using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Xml.Linq;

internal static class Program
{
    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

    private const int OverlayIconIndex = 98;

    private static int Main(string[] args)
    {
        var repoRoot = args.Length > 0 ? args[0] : ".";
        var guiIconPath = Path.Combine(repoRoot, "WindowExplorerSession.gui", "Assets", "WindowExplorerSession.gui.ico");
        var cliIconPath = Path.Combine(repoRoot, "WindowExplorerSession.cli", "Assets", "WindowExplorerSession.cli.ico");
        var guiProjectPath = Path.Combine(repoRoot, "WindowExplorerSession.gui", "WindowExplorerSession.gui.csproj");
        var cliProjectPath = Path.Combine(repoRoot, "WindowExplorerSession.cli", "WindowExplorerSession.cli.csproj");

        Directory.CreateDirectory(Path.GetDirectoryName(guiIconPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(cliIconPath)!);

        var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
        var pngFrames = new List<(int Size, byte[] Data)>(sizes.Length);

        foreach (var size in sizes)
        {
            pngFrames.Add((size, RenderFramePng(size)));
        }

        WriteIco(guiIconPath, pngFrames);
        WriteIco(cliIconPath, pngFrames);

        EnsureProjectIconSettings(
            guiProjectPath,
            "Assets\\WindowExplorerSession.gui.ico",
            includeContentCopy: true,
            contentPath: "Assets\\WindowExplorerSession.gui.ico");
        EnsureProjectIconSettings(
            cliProjectPath,
            "Assets\\WindowExplorerSession.cli.ico",
            includeContentCopy: false,
            contentPath: null);

        Console.WriteLine($"Icon written: {guiIconPath}");
        Console.WriteLine($"Icon written: {cliIconPath}");
        Console.WriteLine("Project icon settings updated for GUI and CLI.");
        return 0;
    }

    private static void EnsureProjectIconSettings(
        string projectPath,
        string applicationIconPath,
        bool includeContentCopy,
        string? contentPath)
    {
        var doc = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
        var root = doc.Root;

        if (root is null)
        {
            return;
        }

        var propertyGroup = root.Elements("PropertyGroup").FirstOrDefault();
        if (propertyGroup is null)
        {
            propertyGroup = new XElement("PropertyGroup");
            root.AddFirst(propertyGroup);
        }

        var iconElement = propertyGroup.Element("ApplicationIcon");
        if (iconElement is null)
        {
            propertyGroup.Add(new XElement("ApplicationIcon", applicationIconPath));
        }
        else
        {
            iconElement.Value = applicationIconPath;
        }

        if (includeContentCopy && contentPath is not null)
        {
            var existingContent = root
                .Elements("ItemGroup")
                .Elements("Content")
                .FirstOrDefault(e => string.Equals((string?)e.Attribute("Include"), contentPath, StringComparison.OrdinalIgnoreCase));

            if (existingContent is null)
            {
                var itemGroup = new XElement("ItemGroup");
                var content = new XElement("Content", new XAttribute("Include", contentPath));
                content.Add(new XElement("CopyToOutputDirectory", "PreserveNewest"));
                itemGroup.Add(content);
                root.Add(itemGroup);
            }
            else
            {
                var copyElement = existingContent.Element("CopyToOutputDirectory");
                if (copyElement is null)
                {
                    existingContent.Add(new XElement("CopyToOutputDirectory", "PreserveNewest"));
                }
                else
                {
                    copyElement.Value = "PreserveNewest";
                }
            }
        }

        doc.Save(projectPath);
    }

    private static byte[] RenderFramePng(int size)
    {
        var overlaySize = Math.Max(10, (int)Math.Round(size * 0.68));
        var cornerPadding = Math.Max(0, size / 16);
        var overlayX = size - overlaySize - cornerPadding;
        var overlayY = size - overlaySize - cornerPadding;

        using var folderIcon = GetShellFolderIcon(size);
        using var overlayIcon = GetShellOverlayIcon(overlaySize, OverlayIconIndex);
        using var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(canvas);

        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.DrawIcon(folderIcon, new Rectangle(0, 0, size, size));
        graphics.DrawIcon(overlayIcon, new Rectangle(overlayX, overlayY, overlaySize, overlaySize));

        using var stream = new MemoryStream();
        canvas.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static void WriteIco(string path, IReadOnlyList<(int Size, byte[] Data)> frames)
    {
        using var fs = File.Create(path);
        using var writer = new BinaryWriter(fs);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)frames.Count);

        var imageOffset = 6 + (16 * frames.Count);

        foreach (var frame in frames)
        {
            writer.Write((byte)(frame.Size >= 256 ? 0 : frame.Size));
            writer.Write((byte)(frame.Size >= 256 ? 0 : frame.Size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(frame.Data.Length);
            writer.Write(imageOffset);
            imageOffset += frame.Data.Length;
        }

        foreach (var frame in frames)
        {
            writer.Write(frame.Data);
        }
    }

    private static Icon GetShellFolderIcon(int size)
    {
        var info = new SHFILEINFO();
        var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | (size <= 16 ? SHGFI_SMALLICON : SHGFI_LARGEICON);

        _ = SHGetFileInfo("folder", FILE_ATTRIBUTE_DIRECTORY, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);
        if (info.hIcon == IntPtr.Zero)
        {
            return (Icon)SystemIcons.Application.Clone();
        }

        try
        {
            using var iconFromHandle = Icon.FromHandle(info.hIcon);
            return (Icon)iconFromHandle.Clone();
        }
        finally
        {
            _ = DestroyIcon(info.hIcon);
        }
    }

    private static Icon GetShellOverlayIcon(int size, int iconIndex)
    {
        var shell32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
        var large = new IntPtr[1];
        var small = new IntPtr[1];

        try
        {
            _ = ExtractIconEx(shell32, iconIndex, large, small, 1);
            var handle = size <= 16
                ? (small[0] != IntPtr.Zero ? small[0] : large[0])
                : (large[0] != IntPtr.Zero ? large[0] : small[0]);

            if (handle == IntPtr.Zero)
            {
                return (Icon)SystemIcons.Application.Clone();
            }

            using var iconFromHandle = Icon.FromHandle(handle);
            return (Icon)iconFromHandle.Clone();
        }
        finally
        {
            if (large[0] != IntPtr.Zero)
            {
                _ = DestroyIcon(large[0]);
            }

            if (small[0] != IntPtr.Zero)
            {
                _ = DestroyIcon(small[0]);
            }
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }
}
