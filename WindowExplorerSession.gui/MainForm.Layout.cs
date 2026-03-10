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
}
