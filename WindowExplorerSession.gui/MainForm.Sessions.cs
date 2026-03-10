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
