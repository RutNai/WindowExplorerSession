using System.Runtime.InteropServices;

namespace WindowExplorerSession.Gui;

internal sealed partial class MainForm
{
    private void ConfigureTrayIcon()
    {
        _trayMenu.Items.Add("Restore", null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add("Exit", null, (_, _) => Close());

        _trayIcon.Text = "WindowExplorerSession.gui";
        _trayIcon.Icon = _trayAppIcon;
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = false;
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private static Icon CreateFolderManagementIcon(int size)
    {
        using var folderIcon = GetShellFolderIcon(size);
        using var canvas = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(canvas);

        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.DrawIcon(folderIcon, new Rectangle(0, 0, size, size));

        // Keep the overlay large enough to remain legible in 16x16 tray icons.
        var overlaySize = Math.Max(10, (int)Math.Round(size * 0.68));
        var cornerPadding = Math.Max(0, size / 16);
        var overlayX = size - overlaySize - cornerPadding;
        var overlayY = size - overlaySize - cornerPadding;

        using var overlayIcon = GetShellOverlayIcon(overlaySize);
        graphics.DrawIcon(overlayIcon, new Rectangle(overlayX, overlayY, overlaySize, overlaySize));

        var iconHandle = canvas.GetHicon();
        try
        {
            using var iconFromHandle = Icon.FromHandle(iconHandle);
            return (Icon)iconFromHandle.Clone();
        }
        finally
        {
            _ = DestroyIcon(iconHandle);
        }
    }

    private static Icon GetShellFolderIcon(int size)
    {
        var info = new SHFILEINFO();
        var flags = ShgfiIcon | ShgfiUseFileAttributes | (size <= 16 ? ShgfiSmallIcon : ShgfiLargeIcon);

        _ = SHGetFileInfo(
            "folder",
            FileAttributeDirectory,
            ref info,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            flags);

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

    private static Icon GetShellOverlayIcon(int size)
    {
        var largeIcons = new IntPtr[1];
        var smallIcons = new IntPtr[1];

        try
        {
            _ = ExtractIconEx(
                Shell32LibraryPath,
                Shell32OverlayIconIndex,
                largeIcons,
                smallIcons,
                1);

            var handle = size <= 16
                ? (smallIcons[0] != IntPtr.Zero ? smallIcons[0] : largeIcons[0])
                : (largeIcons[0] != IntPtr.Zero ? largeIcons[0] : smallIcons[0]);

            if (handle == IntPtr.Zero)
            {
                return (Icon)SystemIcons.Application.Clone();
            }

            using var iconFromHandle = Icon.FromHandle(handle);
            return (Icon)iconFromHandle.Clone();
        }
        finally
        {
            if (largeIcons[0] != IntPtr.Zero)
            {
                _ = DestroyIcon(largeIcons[0]);
            }

            if (smallIcons[0] != IntPtr.Zero)
            {
                _ = DestroyIcon(smallIcons[0]);
            }
        }
    }

    private static readonly string Shell32LibraryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");

    // Classic overlapping pages/windows icon from shell32 picker.
    private const int Shell32OverlayIconIndex = 98;

    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiSmallIcon = 0x000000001;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeDirectory = 0x00000010;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(
        string lpszFile,
        int nIconIndex,
        IntPtr[]? phiconLarge,
        IntPtr[]? phiconSmall,
        uint nIcons);

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

    private void MinimizeToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon.Visible = true;
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        _trayIcon.Visible = false;
    }
}
