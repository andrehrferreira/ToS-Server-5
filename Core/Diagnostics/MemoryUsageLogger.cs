using System.Diagnostics;

public static class MemoryUsageLogger
{
    public static void Log(string prefix = "")
    {
        long managed = GC.GetTotalMemory(false);
        long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;

        /*ServerMonitor.Log($"[Memory]{prefix} Managed: {managed / (1024 * 1024)}MB " +
                          $"Private: {privateBytes / (1024 * 1024)}MB ");*/
    }
}
