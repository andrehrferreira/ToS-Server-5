using UnrealBuildTool;
using System.IO;

public class ToS_Network : ModuleRules
{
    public ToS_Network(ReadOnlyTargetRules Target) : base(Target)
    {
        PublicDefinitions.Add("SODIUM_STATIC=1");
        PublicDefinitions.Add("SODIUM_EXPORT=");

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

        string PlatformString = (Target.Platform == UnrealTargetPlatform.Win64) ? "x64" : "Win32";
        string path = Path.Combine(ModuleDirectory, "../ThirdParty/libsodium/Build/Release/" + PlatformString + "/libsodium.lib");
        PublicAdditionalLibraries.Add(path);
    }
}
