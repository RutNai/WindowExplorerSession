using System.Diagnostics;

namespace WindowExplorerSession.Gui;

internal sealed partial class MainForm
{
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
            if (rows.Count == 0)
            {
                _savedWindowsBindingSource.DataSource = new List<SavedWindowRow>();
            }

            _statusLabel.Text = $"{rows.Count} session(s) in '{_manager.GetDefaultSessionDirectory()}'.";

            if (_savedWindowsExpanded)
            {
                LoadSavedWindowsForSelection();
            }
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

    private IReadOnlyList<SavedWindowRow> GetSelectedSavedWindows()
    {
        return _savedWindowsGrid.SelectedRows
            .OfType<DataGridViewRow>()
            .Select(r => r.DataBoundItem)
            .OfType<SavedWindowRow>()
            .GroupBy(r => r.Index)
            .Select(g => g.First())
            .ToList();
    }

    private void ToggleSavedWindowsExplorer()
    {
        _savedWindowsExpanded = !_savedWindowsExpanded;
        _exploreSavedButton.Text = _savedWindowsExpanded ? "Hide Saved Windows" : "Explore Saved Windows";

        if (_savedWindowsExpanded)
        {
            const int detailMinSize = 180;
            _sessionsSplit.Panel2Collapsed = false;
            _sessionsSplit.Panel2MinSize = detailMinSize;

            LoadSavedWindowsForSelection();

            var targetDistance = Math.Max(180, _sessionsSplit.Height / 2);
            var maxDistance = Math.Max(0, _sessionsSplit.Height - _sessionsSplit.Panel2MinSize - _sessionsSplit.SplitterWidth);
            _sessionsSplit.SplitterDistance = Math.Min(targetDistance, maxDistance);
            return;
        }

        _sessionsSplit.Panel2Collapsed = true;
        _sessionsSplit.Panel2MinSize = 0;
    }

    private void LoadSavedWindowsForSelection()
    {
        if (!_savedWindowsExpanded)
        {
            return;
        }

        var selected = GetSelectedSession();
        if (selected is null)
        {
            _savedWindowsBindingSource.DataSource = new List<SavedWindowRow>();
            return;
        }

        try
        {
            var rows = _manager.GetSessionWindows(selected.FilePath)
                .Select(w => new SavedWindowRow
                {
                    Index = w.Index,
                    IndexDisplay = (w.Index + 1).ToString(),
                    Address = w.Address,
                    VirtualDesktopName = string.IsNullOrWhiteSpace(w.VirtualDesktopName) ? "(default)" : w.VirtualDesktopName,
                    MonitorDeviceName = string.IsNullOrWhiteSpace(w.MonitorDeviceName) ? "(unknown)" : w.MonitorDeviceName,
                    Bounds = $"{w.X},{w.Y} {w.Width}x{w.Height}"
                })
                .ToList();

            _savedWindowsBindingSource.DataSource = rows;
        }
        catch (Exception ex)
        {
            _savedWindowsBindingSource.DataSource = new List<SavedWindowRow>();
            ShowError(ex.Message);
        }
    }

    private void RestoreSelectedSavedWindow()
    {
        var selectedSession = GetSelectedSession();
        if (selectedSession is null)
        {
            ShowInfo("Select a session first.");
            return;
        }

        var selectedWindows = GetSelectedSavedWindows();
        if (selectedWindows.Count == 0)
        {
            ShowInfo("Select one or more saved windows to restore.");
            return;
        }

        try
        {
            var restoredCount = 0;
            var orderedWindows = selectedWindows.OrderBy(w => w.Index).ToList();

            foreach (var window in orderedWindows)
            {
                restoredCount += _manager.RestoreSessionWindow(selectedSession.FilePath, window.Index);
            }

            _statusLabel.Text =
                $"Restored {restoredCount} of {orderedWindows.Count} selected saved window(s) from '{selectedSession.FileName}'.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void DeleteSelectedSavedWindow()
    {
        var selectedSession = GetSelectedSession();
        if (selectedSession is null)
        {
            ShowInfo("Select a session first.");
            return;
        }

        var selectedWindows = GetSelectedSavedWindows();
        if (selectedWindows.Count == 0)
        {
            ShowInfo("Select one or more saved windows to delete.");
            return;
        }

        var result = MessageBox.Show(
            $"Delete {selectedWindows.Count} saved window(s) from '{selectedSession.FileName}'?",
            "Confirm Delete Window",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var removedCount = 0;
            foreach (var window in selectedWindows.OrderByDescending(w => w.Index))
            {
                _ = _manager.DeleteSessionWindow(selectedSession.FilePath, window.Index);
                removedCount++;
            }

            RefreshSessions();
            _statusLabel.Text =
                $"Deleted {removedCount} saved window(s) from '{selectedSession.FileName}'.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
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

    private sealed class SavedWindowRow
    {
        public int Index { get; init; }
        public required string IndexDisplay { get; init; }
        public required string Address { get; init; }
        public required string VirtualDesktopName { get; init; }
        public required string MonitorDeviceName { get; init; }
        public required string Bounds { get; init; }
    }
}
