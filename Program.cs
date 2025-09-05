#define DEBUG

using System.Reflection;
using Wormhole;

class Program
{
    public static void Main(string[] args)
    {
        string projectDirectory = GetProjectDirectory();
        string unrealPath = Path.Combine(projectDirectory, "Unreal");

        if (!Directory.Exists(unrealPath))
            throw new FileNotFoundException($"Please create the virtual link with the client first.");
        
        var config = ServerConfig.LoadConfig();
        ushort port = (ushort)int.Parse(args.Length > 1 ? args[1] : config.Network.Port.ToString());

        var testRunner = new TestTool();
        var testPassed = testRunner.RunAllTests();

        if (!testPassed)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("TESTS FAILED: The application did not pass the tests.");
            Console.WriteLine("SERVER STARTUP ABORTED: Fix the failing tests before running the server.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
        else
        {
            Server.Start(port);
        }

        while (true)
        {
            Thread.Sleep(10);
        }
    }

    public static string GetProjectDirectory()
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        var directoryInfo = new DirectoryInfo(assemblyPath);

        while (directoryInfo != null && directoryInfo.Name != "bin")
            directoryInfo = directoryInfo.Parent;

        if (directoryInfo != null && directoryInfo.Parent != null)
            return directoryInfo.Parent.FullName;

        return AppDomain.CurrentDomain.BaseDirectory;
    }
}
