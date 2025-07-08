using System.Diagnostics;

public static class MemoryUsageLogger
{
    public static void Log(string prefix = "")
    {
        long managed = GC.GetTotalMemory(false);
        long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;
        var stats = ByteBufferPool.GetStats();

        ServerMonitor.Log($"[Memory]{prefix} Managed: {managed / (1024 * 1024)}MB " +
                          $"Private: {privateBytes / (1024 * 1024)}MB " +
                          $"BuffersCreated: {stats.created} InPool: {stats.pooled} InUse: {stats.created - stats.pooled}");
    }
}
