static class Debug
{
    public static void Log(string message)
    {
#if DEBUG
        Console.WriteLine(message);
#endif

        FileLogger.Log(message);
    }
}