using System.IO;
using UnrealBuildTool;

public class ToS_Network : ModuleRules
{
	public ToS_Network(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(new string[] {
			"Core",
            "Sockets",
            "Networking"
        });
					
		PrivateDependencyModuleNames.AddRange(new string[] {
			"CoreUObject",
			"Engine",
			"Slate",
			"SlateCore"
		});
    }
}
