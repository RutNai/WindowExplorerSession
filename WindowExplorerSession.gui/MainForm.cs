using System.Diagnostics;
using WindowExplorerSession.Core;

namespace WindowExplorerSession.Gui;

internal sealed class MainForm : Form
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

    public MainForm()
    {
        Text = "WindowExplorerSession.gui";
        Width = 980;
        Height = 620;
        MinimumSize = new Size(860, 500);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        RefreshSessions();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true
        };

        ConfigureButton(_refreshButton, "Refresh", (_, _) => RefreshSessions());
        ConfigureButton(_saveButton, "Save Current", (_, _) => SaveCurrentSession());
        ConfigureButton(_restoreButton, "Restore Selected", (_, _) => RestoreSelectedSession());
        ConfigureButton(_deleteButton, "Delete Selected", (_, _) => DeleteSelectedSession());
        ConfigureButton(_openFolderButton, "Open Folder", (_, _) => OpenSessionFolder());

        actions.Controls.AddRange(new Control[]
        {
            _refreshButton,
            _saveButton,
            _restoreButton,
            _deleteButton,
            _openFolderButton
        });

        _sessionsGrid.Dock = DockStyle.Fill;
        _sessionsGrid.ReadOnly = true;
        _sessionsGrid.MultiSelect = false;
        _sessionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _sessionsGrid.AutoGenerateColumns = false;
        _sessionsGrid.AllowUserToAddRows = false;
        _sessionsGrid.AllowUserToDeleteRows = false;
        _sessionsGrid.AllowUserToResizeRows = false;

        _sessionsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "File",
            DataPropertyName = nameof(SessionRow.FileName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 40
        });
        _sessionsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Captured (Local)",
            DataPropertyName = nameof(SessionRow.CapturedAtLocal),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 28
        });
        _sessionsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Windows",
            DataPropertyName = nameof(SessionRow.WindowCount),
            Width = 90
        });
        _sessionsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Last Modified (Local)",
            DataPropertyName = nameof(SessionRow.LastWriteLocal),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 30
        });

        _sessionsGrid.DataSource = _bindingSource;

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(0, 8, 0, 0);

        root.Controls.Add(actions, 0, 0);
        root.Controls.Add(_sessionsGrid, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);

        Controls.Add(root);
    }

    private static void ConfigureButton(Button button, string text, EventHandler onClick)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Margin = new Padding(0, 0, 8, 8);
        button.Click += onClick;
    }

    private void RefreshSessions()
    {
        try
        {
            var rows = _manager.ListSessions()
                .Select(s => new SessionRow
                {
                    FilePath = s.FilePath,
                    FileName = s.FileName,
                    WindowCount = s.WindowCount,
                    LastWriteLocal = s.LastWriteTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    CapturedAtLocal = s.CapturedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a"
                })
                .ToList();

            _bindingSource.DataSource = rows;
            _statusLabel.Text = $"{rows.Count} session(s) in '{_manager.GetDefaultSessionDirectory()}'.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SaveCurrentSession()
    {
        try
        {
            var path = _manager.SaveSession();
            RefreshSessions();
            _statusLabel.Text = $"Saved current Explorer session to '{path}'.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RestoreSelectedSession()
    {
        var selected = GetSelectedSession();
        if (selected is null)
        {
            ShowInfo("Select a session to restore.");
            return;
        }

        try
        {
            var restored = _manager.RestoreSession(selected.FilePath);
            _statusLabel.Text = $"Restored {restored} Explorer windows from '{selected.FileName}'.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void DeleteSelectedSession()
    {
        var selected = GetSelectedSession();
        if (selected is null)
        {
            ShowInfo("Select a session to delete.");
            return;
        }

        var result = MessageBox.Show(
            $"Delete '{selected.FileName}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _manager.DeleteSession(selected.FilePath);
            RefreshSessions();
            _statusLabel.Text = $"Deleted '{selected.FileName}'.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenSessionFolder()
    {
        var directory = _manager.GetDefaultSessionDirectory();
        Directory.CreateDirectory(directory);

        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }

    private SessionRow? GetSelectedSession()
    {
        if (_sessionsGrid.CurrentRow?.DataBoundItem is SessionRow row)
        {
            return row;
        }

        return null;
    }

    private void ShowError(string message)
    {
        _statusLabel.Text = message;
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ShowInfo(string message)
    {
        _statusLabel.Text = message;
        MessageBox.Show(message, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed class SessionRow
    {
        public required string FilePath { get; init; }
        public required string FileName { get; init; }
        public required string LastWriteLocal { get; init; }
        public required string CapturedAtLocal { get; init; }
        public int WindowCount { get; init; }
    }
}
