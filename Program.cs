#define DEBUG

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Core.Config;

class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] Unhandled exception: {e.ExceptionObject}");
            Console.ResetColor();
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] Unobserved task exception: {e.Exception}");
            Console.ResetColor();
            e.SetObserved();
        };

        string projectDirectory = GetProjectDirectory();
        string packageJsonPath = Path.Combine(projectDirectory, "package.json");
        string unrealPath = Path.Combine(projectDirectory, "Unreal");
        string currentVersion = VersionReader.GetProjectVersion(packageJsonPath);

        if (!Directory.Exists(unrealPath))
            throw new FileNotFoundException($"Please create the virtual link with the client first.");

        IntegritySystem.InitializeIntegrityTable(packageJsonPath, unrealPath);

        // Load server configuration
        Console.WriteLine("[INIT] Loading server configuration...");
        var config = ServerConfig.LoadConfig();

        // Use port from configuration, but allow command line override
        int portArg = int.Parse(args.Length > 1 ? args[1] : config.Network.Port.ToString());
        int port = portArg;

    #if DEBUG
        //System tests
        var testRunner = new TestTool();
        var testPassed = testRunner.RunAllTests();

        if (testPassed)
        {
            ContractTranspiler.Generate();
            UnrealTranspiler.Generate();
        }
        else
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ TESTS FAILED: The application did not pass the tests.");
            Console.WriteLine("❌ SERVER STARTUP ABORTED: Fix the failing tests before running the server.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
#else
        Console.WriteLine("Running in release mode. Tests and transpilers are skipped.");
#endif

        Guard(() =>
        {
            // Initialize server with configuration
            Console.WriteLine($"[INIT] Starting server on port {port}...");
            UDPServer.Init(port, (UDPSocket socket, string token) =>
            {
                // Check password protection if enabled
                if (config.Server.EnablePasswordProtection)
                {
                    if (string.IsNullOrEmpty(token) || token != config.Server.Password)
                    {
                        Console.WriteLine($"[AUTH] ❌ Invalid password from {socket.RemoteAddress}");
                        return false;
                    }
                    Console.WriteLine($"[AUTH] ✅ Valid password from {socket.RemoteAddress}");
                }

                if (!string.IsNullOrEmpty(token) && !config.Server.EnablePasswordProtection)
                {
                    // Token provided but no password protection - reject for security
                    return false;
                }

                var map = World.GetOrCreate("Sample2");

                PlayerController controller;

                if(!PlayerController.TryGet(socket.Id, out controller))
                {
                    controller = new PlayerController(socket, token);
                    PlayerController.Add(socket.Id, controller);
                }

                map.AddPlayer(controller);

                Console.WriteLine($"[PLAYER] ✅ Player {socket.Id} connected from {socket.RemoteAddress}");
                return true;
            }, new UDPServerOptions()
            {
                Version = VersionReader.ToUInt(currentVersion),
                EnableWAF = config.Security.EnableWAF,
                EnableIntegrityCheck = config.Security.EnableIntegrityCheck,
                MaxConnections = (uint)config.Server.MaxPlayers,
                ReceiveBufferSize = config.Network.ReceiveBufferSize,
                SendBufferSize = config.Network.SendBufferSize,
                SendThreadCount = config.Network.SendThreadCount,
            });

            //ServerMonitor.Start();
        });

        while (true)
        {
            Thread.Sleep(1000);
        }
    }

    [HandleProcessCorruptedStateExceptions]
    public static void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (AccessViolationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] Access violation: {ex.Message}");
            Console.WriteLine(ex);
            Console.ResetColor();
        }
        catch (SEHException ex)
        {
            Thread.Sleep(1000);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] SEH exception: {ex.Message}");
            Console.WriteLine(ex);
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] {ex.Message}");
            Console.WriteLine(ex);
            Console.ResetColor();
        }
    }

    protected static string GetProjectDirectory()
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
