namespace WindowExplorerSession.Gui;

internal sealed partial class MainForm
{
    private static readonly string AppIconPath = Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "WindowExplorerSession.gui.ico");

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

    private static Icon LoadApplicationIcon()
    {
        if (File.Exists(AppIconPath))
        {
            using var icon = new Icon(AppIconPath);
            return (Icon)icon.Clone();
        }

        return (Icon)SystemIcons.Application.Clone();
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
