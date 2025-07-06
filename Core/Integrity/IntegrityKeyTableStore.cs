using System.Collections.Concurrent;

public class IntegrityKeyTableStore
{
    private readonly ConcurrentDictionary<string, IntegrityKeyTable> _tables = new();
    public string MinimumAllowedVersion { get; set; }
    private readonly string _storageFolder;
    private readonly string _unrealFolder;

    public IntegrityKeyTableStore(
        string minimumAllowedVersion,
        string storageFolder = "./Tables",
        string unrealFolder = "./Unreal"
    )
    {
        MinimumAllowedVersion = minimumAllowedVersion;
        _storageFolder = storageFolder;
        _unrealFolder = unrealFolder;

        if (!Directory.Exists(_storageFolder))
            Directory.CreateDirectory(_storageFolder);

        LoadAllTables(_unrealFolder);
    }

    private void LoadAllTables(string unrealFolder)
    {
        foreach (var file in Directory.GetFiles(_storageFolder, "*.bin"))
        {
            try
            {
                var table = IntegrityKeyTable.LoadFromFile(file);
                _tables[table.Version] = table;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Integrity] Failed to load {file}: {ex.Message}");
            }
        }

        GenerateUnrealHeader(unrealFolder);
    }

    private void GenerateUnrealHeader(string unrealFolder)
    {
        try
        {
            string headerPath = Path.Combine(unrealFolder, "Source", "ToS_Network", "Public", "IntegrityTable.h");
            string headerDir = Path.GetDirectoryName(headerPath);

            if (!Directory.Exists(headerDir))
            {
                Directory.CreateDirectory(headerDir);
            }

            var headerContent = GenerateHeaderContent();
            File.WriteAllText(headerPath, headerContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Integrity] Failed to generate header file: {ex.Message}");
        }
    }

    private string GenerateHeaderContent()
    {
        var content = new System.Text.StringBuilder();

        content.AppendLine("#pragma once");
        content.AppendLine();
        content.AppendLine("#include \"CoreMinimal.h\"");
        content.AppendLine();
        content.AppendLine("namespace IntegrityTableData");
        content.AppendLine("{");

        IntegrityKeyTable latestTable = null;
        Version latestVersion = null;

        foreach (var table in _tables.Values)
        {
            try
            {
                var currentVersion = Version.Parse(table.Version);
                if (latestVersion == null || currentVersion > latestVersion)
                {
                    latestVersion = currentVersion;
                    latestTable = table;
                }
            }
            catch (Exception)
            {
                if (latestTable == null || string.Compare(table.Version, latestTable.Version, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    latestTable = table;
                }
            }
        }

        if (latestTable != null)
        {
            content.AppendLine($"    const FString CurrentVersion = TEXT(\"{latestTable.Version}\");");
            content.AppendLine($"    const int32 KeyCount = {latestTable.KeyCount};");

            if (latestTable.Keys != null && latestTable.Keys.Length > 0)
            {
                content.AppendLine("    const int16 Keys[] = {");

                for (int i = 0; i < latestTable.Keys.Length; i++)
                {
                    if (i % 16 == 0)
                        content.Append("        ");

                    content.Append($"{latestTable.Keys[i]}");

                    if (i < latestTable.Keys.Length - 1)
                        content.Append(", ");

                    if (i % 16 == 15 || i == latestTable.Keys.Length - 1)
                        content.AppendLine();
                }

                content.AppendLine("    };");
            }
            else
            {
                content.AppendLine("    const int16* Keys = nullptr;");
                content.AppendLine("    const int32 KeyCount = 0;");
            }
        }
        else
        {
            content.AppendLine("    const FString CurrentVersion = TEXT(\"\");");
            content.AppendLine("    const int32 KeyCount = 0;");
            content.AppendLine("    const int16* Keys = nullptr;");
        }

        content.AppendLine();

        // Simple access functions
        content.AppendLine("    inline int16 GetKey(int32 Index)");
        content.AppendLine("    {");
        content.AppendLine("        if (Index < 0 || Index >= KeyCount || Keys == nullptr)");
        content.AppendLine("            return 0;");
        content.AppendLine("        return Keys[Index];");
        content.AppendLine("    }");
        content.AppendLine();

        content.AppendLine("    inline bool IsValidIndex(int32 Index)");
        content.AppendLine("    {");
        content.AppendLine("        return Index >= 0 && Index < KeyCount && Keys != nullptr;");
        content.AppendLine("    }");
        content.AppendLine();

        content.AppendLine("    inline const int16* GetAllKeys()");
        content.AppendLine("    {");
        content.AppendLine("        return Keys;");
        content.AppendLine("    }");
        content.AppendLine();

        content.AppendLine("    inline int32 GetTotalKeyCount()");
        content.AppendLine("    {");
        content.AppendLine("        return KeyCount;");
        content.AppendLine("    }");
        content.AppendLine();

        content.AppendLine("    inline const FString& GetCurrentVersion()");
        content.AppendLine("    {");
        content.AppendLine("        return CurrentVersion;");
        content.AppendLine("    }");
        content.AppendLine();

        content.AppendLine("    #define INTEGRITY_GET_KEY(Index) IntegrityTableData::GetKey(Index)");
        content.AppendLine("    #define INTEGRITY_KEY_COUNT IntegrityTableData::GetTotalKeyCount()");
        content.AppendLine("    #define INTEGRITY_VERSION IntegrityTableData::GetCurrentVersion()");
        content.AppendLine("    #define INTEGRITY_IS_VALID_INDEX(Index) IntegrityTableData::IsValidIndex(Index)");

        content.AppendLine("}");
        content.AppendLine();

        return content.ToString();
    }

    public void AddOrUpdateTable(IntegrityKeyTable table)
    {
        _tables[table.Version] = table;
        string path = Path.Combine(_storageFolder, $"{table.Version}.bin");
        table.SaveToFile(path);
        Console.WriteLine($"[Integrity] Saved table for version {table.Version} to disk.");
        GenerateUnrealHeader(_unrealFolder);
    }

    public bool TryGetTable(string version, out IntegrityKeyTable table)
    {
        return _tables.TryGetValue(version, out table);
    }

    public bool IsVersionAllowed(string version)
    {
        Version v = Version.Parse(version);
        Version min = Version.Parse(MinimumAllowedVersion);
        return v >= min;
    }
}
