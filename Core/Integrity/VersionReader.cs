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

    public static uint ToUInt(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version string is null or empty.", nameof(version));

        var parts = version.Split('.');

        if (parts.Length != 3)
            throw new FormatException("Version string must be in the format 'X.Y.Z'.");

        if (!byte.TryParse(parts[0], out byte major) ||
            !byte.TryParse(parts[1], out byte minor) ||
            !ushort.TryParse(parts[2], out ushort patch))
            throw new FormatException("Invalid number format in version string.");

        uint result = (uint)(major << 24) | (uint)(minor << 16) | patch;
        return result;
    }
}
