using System.Diagnostics;
using System.Reflection;


class Program
{
    public static void Main(string[] args)
    {
        string projectDirectory = GetProjectDirectory();
        string packageJsonPath = Path.Combine(projectDirectory, "package.json");
        string unrealPath = Path.Combine(projectDirectory, "Unreal");
        string currentVersion = VersionReader.GetProjectVersion(packageJsonPath);

        if (!Directory.Exists(unrealPath))
            throw new FileNotFoundException($"Please create the virtual link with the client first.");

        IntegritySystem.InitializeIntegrityTable(packageJsonPath, unrealPath);

        int portArg = int.Parse(args.Length > 1 ? args[1] : "3565");
        int port = portArg;

#if DEBUG
        //System tests
        var testRunner = new TestTool();
        var testPassed = testRunner.RunAllTests();

        if (testPassed)
        {
            //Generate
            ContractTraspiler.Generate();
            //UnrealTraspiler.Generate(clientProjectName);
            //UnrealUtils.RegenerateVSCodes(clientPath, unrealEditorPath, clientProjectName);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("As the application did not pass the tests, the contract files and files for Unreal were not generated..");
        }
#else
        Console.WriteLine("Running in release mode. Tests and transpilers are skipped.");
#endif

        ByteBufferPool.PreAllocate(1024); 

        World world = new World(100000);

        UDPServer.SetWorld(world);

        UDPServer.Init(port, (UDPSocket socket, string token) =>
        {
            if (!string.IsNullOrEmpty(token))
            {
                /*if (token == "valid-token")
                {
                    int entityId = UDPServer.GetWorld().SpawnEntity($"Player_{socket.Id}", new FVector(), new FRotator());

                    socket.EntityId = entityId;

                    return true;
                }*/

                return false;
            }

            return false;
        }, new UDPServerOptions()
        {
            Version = VersionReader.ToUInt(currentVersion),
            EnableWAF = false,
            EnableIntegrityCheck = true, 
            MaxConnections = 25000,
            ReceiveBufferSize = 512 * 1024, 
            SendBufferSize = 512 * 1024,
        });

        ServerMonitor.Start();

        while (true)
        {
            Thread.Sleep(1000);
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
