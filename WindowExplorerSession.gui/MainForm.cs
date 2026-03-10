using WindowExplorerSession.Core;

namespace WindowExplorerSession.Gui;

internal sealed partial class MainForm : Form
{
    private readonly SessionManager _manager = new();
    private readonly DataGridView _sessionsGrid = new();
    private readonly BindingSource _bindingSource = new();
    private readonly Button _refreshButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _restoreButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _openFolderButton = new();
    private readonly Label _statusLabel = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly Icon _windowIcon;
    private readonly Icon _trayAppIcon;

    public MainForm()
    {
        Text = "WindowExplorerSession.gui";
        Width = 980;
        Height = 620;
        MinimumSize = new Size(860, 500);
        StartPosition = FormStartPosition.CenterScreen;

        _windowIcon = LoadApplicationIcon();
        _trayAppIcon = (Icon)_windowIcon.Clone();
        Icon = _windowIcon;

        BuildLayout();
        ConfigureTrayIcon();
        RefreshSessions();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        };

        FormClosed += (_, _) =>
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayMenu.Dispose();
            _windowIcon.Dispose();
            _trayAppIcon.Dispose();
        };
    }
}
