using WindowExplorerSession.Core;

namespace WindowExplorerSession.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var filePathArg = args.Length > 1 ? args[1] : null;
        var manager = new SessionManager();

        try
        {
            return command switch
            {
                "save" => Save(manager, filePathArg),
                "load" => Load(manager, filePathArg),
                "list" => List(manager),
                _ => InvalidCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int Save(SessionManager manager, string? filePathArg)
    {
        var path = manager.SaveSession(filePathArg);
        Console.WriteLine($"Saved Explorer session to '{path}'.");
        return 0;
    }

    private static int Load(SessionManager manager, string? filePathArg)
    {
        var restored = manager.RestoreSession(filePathArg);
        var path = string.IsNullOrWhiteSpace(filePathArg) ? manager.GetLatestSessionPath() : filePathArg;
        Console.WriteLine($"Restored {restored} Explorer windows from '{path}'.");
        return 0;
    }

    private static int List(SessionManager manager)
    {
        var sessions = manager.ListSessions();
        if (sessions.Count == 0)
        {
            Console.WriteLine($"No session files found in '{manager.GetDefaultSessionDirectory()}'.");
            return 0;
        }

        Console.WriteLine($"Sessions in '{manager.GetDefaultSessionDirectory()}':");
        foreach (var session in sessions)
        {
            var captured = session.CapturedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
            Console.WriteLine($"- {session.FileName} | windows={session.WindowCount} | captured={captured}");
        }

        return 0;
    }

    private static int InvalidCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: '{command}'");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  WindowExplorerSession.cli save [sessionFile]");
        Console.WriteLine("  WindowExplorerSession.cli load [sessionFile]");
        Console.WriteLine("  WindowExplorerSession.cli list");
    }
}
