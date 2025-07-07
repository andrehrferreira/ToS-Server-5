using System;
using System.Diagnostics;
using System.Threading;

public static class ServerMonitor
{
    private static Thread _thread;
    private static bool _running;
    private static TimeSpan _lastCpu;
    private static DateTime _lastCheck;

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
        Console.Clear();

        while (_running)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Tales Of Shadowland Server Monitor");
            Console.WriteLine("------------------------------------");
            PrintStats();
            Thread.Sleep(1000);
        }
    }

    private static void PrintStats()
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

        Console.WriteLine($"Connections:       {connections}");
        Console.WriteLine($"Packets Tx/Rx:     {packetsSent} / {packetsReceived}");
        Console.WriteLine($"Traffic Tx/Rx:     {bytesSent / 1024}KB / {bytesReceived / 1024}KB");
        Console.WriteLine($"Memory Managed:    {managed / (1024 * 1024)} MB");
        Console.WriteLine($"Memory Private:    {privateBytes / (1024 * 1024)} MB");
        Console.WriteLine($"Buffers Created:   {created}  In Use: {created - pooled}");
        Console.WriteLine($"GC Collections:    {GC.CollectionCount(0)} / {GC.CollectionCount(1)} / {GC.CollectionCount(2)}");
        Console.WriteLine($"CPU Usage:         {cpuUsage:F2}%");
    }
}
