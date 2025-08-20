using UnrealBuildTool;
using System.IO;

public class ToS_Network : ModuleRules
{
    public ToS_Network(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(new[]
        {
            "Core", "Sockets", "Networking", "Engine", "InputCore", "EnhancedInput"
        });

        PrivateDependencyModuleNames.AddRange(new[]
        {
            "CoreUObject", "Slate", "SlateCore", "Projects"
        });

        string PluginRoot = Path.GetFullPath(Path.Combine(ModuleDirectory, ".."));
        string SodiumRoot = Path.Combine(PluginRoot, "ThirdParty", "libsodium");
        string IncludePath = Path.Combine(SodiumRoot, "src", "libsodium", "include");

        if (!Directory.Exists(IncludePath))
            throw new BuildException($"libsodium include not found at: {IncludePath}");

        PublicIncludePaths.Add(IncludePath);

        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            string LibDir = Path.Combine(SodiumRoot, "Build", "Release", "x64");
            string ImportLib = Path.Combine(LibDir, "libsodium.lib");
            string DllPath = Path.Combine(LibDir, "libsodium.dll");

            if (!File.Exists(ImportLib))
                throw new BuildException($"libsodium import lib not found: {ImportLib}");
            
            PublicAdditionalLibraries.Add(ImportLib);

            if (File.Exists(DllPath))
            {
                PublicDelayLoadDLLs.Add("libsodium.dll");
                RuntimeDependencies.Add($"$(TargetOutputDir)/libsodium.dll", DllPath);
            }
                
            //PublicSystemLibraries.AddRange(new[] { "ws2_32", "advapi32", "user32", "bcrypt" });
        }
    }
}
