# WindowExplorerSession

Utilities to save, browse, and restore File Explorer window sessions on Windows.

Target framework: .NET 10 (`net10.0-windows`).

## Projects

- `WindowExplorerSession.core`: Shared library with capture, restore, and session-file management logic.
- `WindowExplorerSession.cli`: Command-line app for save/load/list operations.
- `WindowExplorerSession.gui`: WinForms desktop app to list, browse, and manage sessions.

## Build

```powershell
dotnet build .\WindowExplorerSession.sln
```

## CLI Usage

```powershell
# Save session to a new default file:
# %LOCALAPPDATA%\WindowExplorerSession\[YYYY-MM-DD_HH-mm-ss]_session.json
dotnet run --project .\WindowExplorerSession.cli -- save

# Load session from newest file in:
# %LOCALAPPDATA%\WindowExplorerSession\
dotnet run --project .\WindowExplorerSession.cli -- load

# List available sessions
dotnet run --project .\WindowExplorerSession.cli -- list

# Use a custom session file
dotnet run --project .\WindowExplorerSession.cli -- save "D:\sessions\explorer-session.json"
dotnet run --project .\WindowExplorerSession.cli -- load "D:\sessions\explorer-session.json"
```

## GUI Usage

```powershell
dotnet run --project .\WindowExplorerSession.gui
```

The GUI supports:
- refresh session list
- save current Explorer session
- restore selected session
- delete selected session file
- open the session storage folder

## Notes

- Virtual desktop restore is name-based. If a desktop name is missing or renamed, restore still proceeds for size, position, and address.
- Special shell pages may not always map to a standard filesystem path.
