using System;
using System.Diagnostics;

public static class MemoryUsageLogger
{
    public static void Log(string prefix = "")
    {
        long managed = GC.GetTotalMemory(false);
        long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;
        var stats = ByteBufferPool.GetStats();
        Console.WriteLine($"[Memory]{prefix} Managed: {managed / (1024 * 1024)}MB " +
                          $"Private: {privateBytes / (1024 * 1024)}MB " +
                          $"BuffersCreated: {stats.created} InPool: {stats.pooled} InUse: {stats.created - stats.pooled}");
    }
}
