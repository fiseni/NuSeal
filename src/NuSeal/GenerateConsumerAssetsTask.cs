using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace NuSeal;

public partial class GenerateConsumerAssetsTask : Task
{
    [Required]
    public string NuSealAssetsPath { get; set; } = "";

    [Required]
    public string NuSealVersion { get; set; } = "";

    [Required]
    public string ConsumerOutputPath { get; set; } = "";

    [Required]
    public string ConsumerPackageId { get; set; } = "";

    [Required]
    public string ConsumerAssemblyName { get; set; } = "";

    public string? ConsumerPropsFile { get; set; }

    public string? ConsumerTargetsFile { get; set; }

    public string? ValidationMode { get; set; }

    public string? ValidationScope { get; set; }

    public override bool Execute()
    {
        if (string.IsNullOrEmpty(NuSealVersion))
        {
            Log.LogError("NuSeal: The version of NuSeal package can not be determined!");
            return false;
        }

        if (string.IsNullOrEmpty(ConsumerOutputPath))
        {
            Log.LogError("NuSeal: The value of $(OutputPath) is empty!");
            return false;
        }

        if (string.IsNullOrEmpty(ConsumerPackageId))
        {
            Log.LogError("NuSeal: The PackageId property must be defined!");
            return false;
        }

        if (string.IsNullOrEmpty(ConsumerAssemblyName))
        {
            Log.LogError("NuSeal: The value of $(AssemblyName) is empty!");
            return false;
        }

        try
        {
            var parameters = new ConsumerParameters(
                nuSealAssetsPath: NuSealAssetsPath,
                nuSealVersion: NuSealVersion,
                outputPath: ConsumerOutputPath,
                packageId: ConsumerPackageId,
                assemblyName: ConsumerAssemblyName,
                propsFile: ConsumerPropsFile,
                targetsFile: ConsumerTargetsFile,
                validationMode: ValidationMode,
                validationScope: ValidationScope);

            var props = Assets.GenerateProps(parameters);
            var targets = Assets.GenerateTargets(parameters);

            props = Assets.AppendConsumerAsset(props, parameters.PropsFile);
            targets = Assets.AppendConsumerAsset(targets, parameters.TargetsFile);

            var propsOutputFile = Path.Combine(parameters.OutputPath, $"{parameters.PackageId}.props");
            var targetsOutputFile = Path.Combine(parameters.OutputPath, $"{parameters.PackageId}.targets");

            File.WriteAllText(propsOutputFile, props);
            File.WriteAllText(targetsOutputFile, targets);
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
