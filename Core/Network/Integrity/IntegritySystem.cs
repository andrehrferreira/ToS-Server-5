using System.Reflection;

public static class IntegritySystem
{
    public static void InitializeIntegrityTable(string packageJsonPath, string unrealFolder = "./Unreal")
    {
        string currentVersion = VersionReader.GetProjectVersion(packageJsonPath);
        string projectDirectory = GetProjectDirectory();
        string integrityTables = Path.Combine(projectDirectory, "Tables");

        var store = new IntegrityKeyTableStore(minimumAllowedVersion: "5.0.0", integrityTables, unrealFolder);

        if (!store.TryGetTable(currentVersion, out var table))
        {
            table = IntegrityKeyTableGenerator.GenerateTable(currentVersion);
            store.AddOrUpdateTable(table);
            Console.WriteLine($"[Integrity] Generated and saved new table for version {currentVersion}.");
        }
        else
        {
            Console.WriteLine($"[Integrity] Table for version {currentVersion} already exists and is loaded.");
        }
    }

    public static bool GetCurrentTable(out IntegrityKeyTable table)
    {
        string projectDirectory = GetProjectDirectory();
        string integrityTables = Path.Combine(projectDirectory, "Tables");
        string currentVersion = VersionReader.GetProjectVersion(Path.Combine(projectDirectory, "package.json"));
        var store = new IntegrityKeyTableStore(minimumAllowedVersion: "5.0.0", integrityTables);
        
        if (store.TryGetTable(currentVersion, out table))           
            return true;

        return false;
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
