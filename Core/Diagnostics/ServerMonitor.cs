using Spectre.Console;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class ServerMonitor
{
    private static Thread _thread;
    private static bool _running;
    private static TimeSpan _lastCpu;
    private static DateTime _lastCheck;
    private static List<string> _logs = new();
    private static readonly object _logLock = new();

    public static void Start()
    {
        Console.Clear();

        if (_running)
            return;

        _running = true;
        _lastCpu = Process.GetCurrentProcess().TotalProcessorTime;
        _lastCheck = DateTime.UtcNow;

        var cpuMemTable = new Table().NoBorder().Expand();
        cpuMemTable.AddColumn(new TableColumn("[u]Usage[/]").LeftAligned());
        cpuMemTable.AddColumn(new TableColumn("[u]Graph[/]").LeftAligned());

        var metricsTable = new Table().NoBorder().Expand();
        metricsTable.AddColumn(new TableColumn("[u]Metric[/]").LeftAligned());
        metricsTable.AddColumn(new TableColumn("[u]Value[/]").RightAligned());

        var logsTable = new Table().NoBorder().Expand();
        logsTable.AddColumn(new TableColumn("[u]Logs[/]").LeftAligned());

        var layout = new Table().Centered();
        layout.AddColumn("[bold green]Tales Of Shadowland Server Monitor[/]");
        layout.AddRow(new Rule("[yellow]CPU & Memory Usage[/]"));
        layout.AddRow(cpuMemTable);
        layout.AddRow(new Rule("[yellow]Metrics[/]"));
        layout.AddRow(metricsTable);
        layout.AddRow(new Rule("[yellow]Logs[/]"));
        layout.AddRow(logsTable);

        AnsiConsole.Live(layout)
        .Start(ctx =>
        {
            while (_running)
            {
                UpdateCpuAndMemory(cpuMemTable);
                UpdateMetrics(metricsTable);
                UpdateLogs(logsTable);

                ctx.Refresh();
                Thread.Sleep(1000);
            }
        });
    }

    public static void Stop() => _running = false;

    public static void Log(string message)
    {
        lock (_logLock)
        {
            _logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_logs.Count > 10)
                _logs.RemoveAt(0);
        }
    }

    private static void UpdateCpuAndMemory(Table table)
    {
        double cpuUsage = GetCpuUsage();
        long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;
        double memUsage = GetMemoryUsagePercentage(privateBytes, out double totalGB);

        table.Rows.Clear();

        int cpuBlocks = Math.Clamp((int)(cpuUsage / 5), 1, 20);
        int memBlocks = Math.Clamp((int)(memUsage / 5), 1, 20);

        string cpuBar = $"[green]{new string('█', cpuBlocks).PadRight(20)}[/] {cpuUsage:F1}%";
        string memBar = $"[blue]{new string('█', memBlocks).PadRight(20)}[/] {memUsage:F1}% of {totalGB:F1} GB";

        table.AddRow("CPU Usage", cpuBar);
        table.AddRow("Memory Usage", memBar);
    }

    private static void UpdateMetrics(Table table)
    {
        table.Rows.Clear();

        int connections = UDPServer.ConnectionCount;
        long packetsSent = UDPServer.PacketsSent;
        long packetsReceived = UDPServer.PacketsReceived;
        long bytesSent = UDPServer.BytesSent;
        long bytesReceived = UDPServer.BytesReceived;
        int tickRate = UDPServer.TickRate;

        long managed = GC.GetTotalMemory(false);
        long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;

        table.AddRow("Connections", connections.ToString());
        table.AddRow("Packets Tx/Rx", $"{packetsSent} / {packetsReceived}");
        table.AddRow("Traffic Tx/Rx", $"{bytesSent / 1024} KB / {bytesReceived / 1024} KB");
        table.AddRow("Tick Rate", tickRate.ToString());
        table.AddRow("Managed Memory", $"{managed / (1024 * 1024)} MB");
        table.AddRow("Private Memory", $"{privateBytes / (1024 * 1024)} MB");
        table.AddRow("GC Collections", $"{GC.CollectionCount(0)} / {GC.CollectionCount(1)} / {GC.CollectionCount(2)}");
        table.AddRow("Queue Sending Count", UDPServer._sendQueueCount.ToString());
    }

    private static void UpdateLogs(Table table)
    {
        table.Rows.Clear();

        lock (_logLock)
        {
            if (_logs.Count == 0)
                table.AddRow("[grey]No logs yet...[/]");
            else
                foreach (var log in _logs)
                    table.AddRow(log.EscapeMarkup());
        }
    }

    private static double GetCpuUsage()
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan cpu = Process.GetCurrentProcess().TotalProcessorTime;
        double delta = (now - _lastCheck).TotalSeconds;
        double cpuUsage = 0.0;

        if (delta > 0)
            cpuUsage = (cpu - _lastCpu).TotalSeconds / (Environment.ProcessorCount * delta) * 100.0;

        _lastCpu = cpu;
        _lastCheck = now;
        return cpuUsage;
    }

    private static double GetMemoryUsagePercentage(long appPrivateBytes, out double totalGB)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                totalGB = memStatus.ullTotalPhys / (1024.0 * 1024 * 1024);
                return appPrivateBytes / (double)memStatus.ullTotalPhys * 100.0;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var lines = System.IO.File.ReadAllLines("/proc/meminfo");
                var line = lines.FirstOrDefault(l => l.StartsWith("MemTotal"));
                if (line != null)
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    double totalKb = double.Parse(parts[1]);
                    double total = totalKb * 1024;
                    totalGB = total / (1024.0 * 1024 * 1024);
                    return appPrivateBytes / total * 100.0;
                }
            }
            catch { }
        }

        totalGB = 0;
        return 0;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
