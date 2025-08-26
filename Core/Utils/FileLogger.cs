using System;
using System.IO;
using System.Text;

public static class FileLogger
{
    private static string _serverLogPath = @"G:\ToS\Server\server_debug.log";
    private static readonly object _lock = new object();

        static FileLogger()
    {
        // Clear log file on startup
        ClearLog();

        // Log the file location to console
        Console.WriteLine($"[LOG] Server debug log: {_serverLogPath}");

        // Add session header
        var sessionHeader = $"=== SERVER SESSION STARTED: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}";
        File.AppendAllText(_serverLogPath, sessionHeader, Encoding.UTF8);
    }

    public static void ClearLog()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_serverLogPath))
                    File.Delete(_serverLogPath);
            }
            catch { }
        }
    }

    public static void Log(string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logLine = $"[{timestamp}] {message}\n";
                File.AppendAllText(_serverLogPath, logLine, Encoding.UTF8);
            }
            catch { }
        }
    }

    public static void LogHex(string prefix, byte[] data)
    {
        if (data == null || data.Length == 0) return;

        var hex = Convert.ToHexString(data);
        Log($"{prefix}: {hex}");
    }

    public static void LogHex(string prefix, ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return;

        var hex = Convert.ToHexString(data);
        Log($"{prefix}: {hex}");
    }
}
