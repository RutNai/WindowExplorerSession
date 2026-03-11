namespace WindowExplorerSession.Gui;

internal sealed partial class MainForm
{
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
        ConfigureButton(_exploreSavedButton, "Explore Saved Windows", (_, _) => ToggleSavedWindowsExplorer());

        actions.Controls.AddRange(new Control[]
        {
            _refreshButton,
            _saveButton,
            _restoreButton,
            _deleteButton,
            _openFolderButton,
            _exploreSavedButton
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
        _sessionsGrid.SelectionChanged += (_, _) => LoadSavedWindowsForSelection();

        _savedWindowsGrid.Dock = DockStyle.Fill;
        _savedWindowsGrid.ReadOnly = true;
        _savedWindowsGrid.MultiSelect = true;
        _savedWindowsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _savedWindowsGrid.AutoGenerateColumns = false;
        _savedWindowsGrid.AllowUserToAddRows = false;
        _savedWindowsGrid.AllowUserToDeleteRows = false;
        _savedWindowsGrid.AllowUserToResizeRows = false;

        _savedWindowsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "#",
            DataPropertyName = nameof(SavedWindowRow.IndexDisplay),
            Width = 52
        });
        _savedWindowsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Address",
            DataPropertyName = nameof(SavedWindowRow.Address),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 46
        });
        _savedWindowsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Desktop",
            DataPropertyName = nameof(SavedWindowRow.VirtualDesktopName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 18
        });
        _savedWindowsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Monitor",
            DataPropertyName = nameof(SavedWindowRow.MonitorDeviceName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 18
        });
        _savedWindowsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Bounds",
            DataPropertyName = nameof(SavedWindowRow.Bounds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 18
        });

        _savedWindowsGrid.DataSource = _savedWindowsBindingSource;

        ConfigureButton(_restoreWindowButton, "Restore Window", (_, _) => RestoreSelectedSavedWindow());
        ConfigureButton(_deleteWindowButton, "Delete Window", (_, _) => DeleteSelectedSavedWindow());

        var windowActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true
        };
        windowActions.Controls.AddRange(new Control[]
        {
            _restoreWindowButton,
            _deleteWindowButton
        });

        var detailPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 8, 0, 0)
        };
        detailPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detailPanel.Controls.Add(windowActions, 0, 0);
        detailPanel.Controls.Add(_savedWindowsGrid, 0, 1);

        _sessionsSplit.Dock = DockStyle.Fill;
        _sessionsSplit.Orientation = Orientation.Horizontal;
        _sessionsSplit.Panel1.Controls.Add(_sessionsGrid);
        _sessionsSplit.Panel2.Controls.Add(detailPanel);
        _sessionsSplit.FixedPanel = FixedPanel.Panel2;
        _sessionsSplit.IsSplitterFixed = false;
        _sessionsSplit.SplitterWidth = 7;
        _sessionsSplit.Panel2MinSize = 0;
        _sessionsSplit.Panel2Collapsed = true;
        _savedWindowsExpanded = false;

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(0, 8, 0, 0);

        root.Controls.Add(actions, 0, 0);
        root.Controls.Add(_sessionsSplit, 0, 1);
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
}
