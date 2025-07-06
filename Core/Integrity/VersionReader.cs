using Newtonsoft.Json.Linq;

public static class VersionReader
{
    public static string GetProjectVersion(string packageJsonPath)
    {
        if (!File.Exists(packageJsonPath))
            throw new FileNotFoundException($"package.json not found at {packageJsonPath}");

        var json = JObject.Parse(File.ReadAllText(packageJsonPath));
        return json["version"]?.ToString() ?? throw new Exception("Version not found in package.json");
    }
}
