using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace NuSeal;

public partial class PrepareAssetsForConsumerTask : Task
{
    [Required]
    public string NuSealAssetsPath { get; set; } = "";

    [Required]
    public string OutputPath { get; set; } = "";

    public string ConsumerPropsFile { get; set; } = "";

    public string ConsumerTargetsFile { get; set; } = "";

    public override bool Execute()
    {
        //Debugger.Launch();

        try
        {
            var nusealProps = Path.Combine(NuSealAssetsPath, "NuSeal.props");
            var nusealTargets = Path.Combine(NuSealAssetsPath, "NuSeal.props");

            var nusealPropsOutput = Path.Combine(OutputPath, "NuSeal.props");
            var nusealTargetsOutput = Path.Combine(OutputPath, "NuSeal.targets");

            File.Copy(nusealProps, nusealPropsOutput, true);
            File.Copy(nusealTargets, nusealTargetsOutput, true);

            var propsContent = File.ReadAllText(nusealPropsOutput);
            propsContent = propsContent.Replace("<NuSealValidateAllTypes>disable</NuSealValidateAllTypes>", "<NuSealValidateAllTypes>enable</NuSealValidateAllTypes>");
            File.WriteAllText(nusealPropsOutput, propsContent);

            if (!string.IsNullOrEmpty(ConsumerPropsFile) && File.Exists(ConsumerPropsFile))
            {
                var content = File.ReadAllText(ConsumerPropsFile);
                File.AppendAllText(nusealPropsOutput, content);
            }

            if (!string.IsNullOrEmpty(ConsumerTargetsFile) && File.Exists(ConsumerTargetsFile))
            {
                var content = File.ReadAllText(ConsumerTargetsFile);
                File.AppendAllText(nusealTargetsOutput, content);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
