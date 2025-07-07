using System;
using System.Diagnostics;
using System.Threading;
using Spectre.Console;

public static class ServerMonitor
{
    private static Thread _thread;
    private static bool _running;
    private static TimeSpan _lastCpu;
    private static DateTime _lastCheck;
    private static Table _table;

    public static void Start()
    {
        if (_running)
            return;

        _running = true;
        _lastCpu = Process.GetCurrentProcess().TotalProcessorTime;
        _lastCheck = DateTime.UtcNow;
        _thread = new Thread(Run) { IsBackground = true };
        _thread.Start();
    }

    public static void Stop()
    {
        _running = false;
    }

    private static void Run()
    {
        _table = CreateTable();
        AnsiConsole.Clear();
        AnsiConsole.Live(_table)
            .Start(ctx =>
            {
                while (_running)
                {
                    UpdateStats();
                    ctx.Refresh();
                    Thread.Sleep(1000);
                }
            });
    }

    private static Table CreateTable()
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.Title = new TableTitle("[yellow]Tales Of Shadowland Server Monitor[/]");
        table.AddColumn("Metric");
        table.AddColumn("Value");
        return table;
    }

    private static void UpdateStats()
    {
        int connections = UDPServer.ConnectionCount;
        long packetsSent = UDPServer.PacketsSent;
        long packetsReceived = UDPServer.PacketsReceived;
        long bytesSent = UDPServer.BytesSent;
        long bytesReceived = UDPServer.BytesReceived;

        var (created, pooled) = ByteBufferPool.GetStats();
        long managed = GC.GetTotalMemory(false);
        long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;

        DateTime now = DateTime.UtcNow;
        TimeSpan cpu = Process.GetCurrentProcess().TotalProcessorTime;
        double cpuUsage = 0.0;
        double delta = (now - _lastCheck).TotalSeconds;
        if (delta > 0)
            cpuUsage = (cpu - _lastCpu).TotalSeconds / (Environment.ProcessorCount * delta) * 100.0;
        _lastCpu = cpu;
        _lastCheck = now;

        _table.Rows.Clear();
        _table.AddRow("Connections", connections.ToString());
        _table.AddRow("Packets Tx/Rx", $"{packetsSent} / {packetsReceived}");
        _table.AddRow("Traffic Tx/Rx", $"{bytesSent / 1024}KB / {bytesReceived / 1024}KB");
        _table.AddRow("Memory Managed", $"{managed / (1024 * 1024)} MB");
        _table.AddRow("Memory Private", $"{privateBytes / (1024 * 1024)} MB");
        _table.AddRow("Buffers Created", $"{created}  In Use: {created - pooled}");
        _table.AddRow("GC Collections", $"{GC.CollectionCount(0)} / {GC.CollectionCount(1)} / {GC.CollectionCount(2)}");
        _table.AddRow("CPU Usage", $"{cpuUsage:F2}%");
    }
}
