using UnrealBuildTool;
using static UnrealBuildTool.ModuleRules;

public class RealTimeExample : ModuleRules
{
    public RealTimeExample(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicIncludePaths.Add(ModuleDirectory);

        PublicDependencyModuleNames.AddRange(new string[] {
            "Core",
            "CoreUObject",
            "Engine",
            "InputCore",
            "Slate",
            "SlateCore",
            "ApplicationCore"
        });

        PrivateDependencyModuleNames.AddRange(new string[] { });

        if (Target.bBuildEditor)
        {
            PrivateDependencyModuleNames.AddRange(new string[] {
                "UnrealEd",
                "LevelEditor"
            });
        }
    }
}