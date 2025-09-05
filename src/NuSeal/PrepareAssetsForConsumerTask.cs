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

            var props = File.ReadAllText(nusealProps);
            var targets = File.ReadAllText(nusealTargets);

            props = props.Replace("<NuSealValidateAllTypes>disable</NuSealValidateAllTypes>", "<NuSealValidateAllTypes>enable</NuSealValidateAllTypes>");
            File.WriteAllText(nusealPropsOutput, props);

            if (!string.IsNullOrEmpty(ConsumerPropsFile) && File.Exists(ConsumerPropsFile))
            {
                var content = File.ReadAllText(ConsumerPropsFile);
                content = RemoveProjectTags(content);
                props = props.Replace("</Project>", $"{content}{Environment.NewLine}</Project>");
            }

            if (!string.IsNullOrEmpty(ConsumerTargetsFile) && File.Exists(ConsumerTargetsFile))
            {
                var content = File.ReadAllText(ConsumerTargetsFile);
                content = RemoveProjectTags(content);
                targets = targets.Replace("</Project>", $"{content}{Environment.NewLine}</Project>");
            }

            File.WriteAllText(nusealPropsOutput, props);
            File.WriteAllText(nusealTargetsOutput, targets);

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    // Very rudimentary, but it's not worth parsing the XML properly for this
    private static string RemoveProjectTags(string content)
    {
        int startIndex = content.IndexOf("<Project");
        if (startIndex == -1)
            return content;
        int endIndex = content.IndexOf(">", startIndex);
        if (endIndex == -1)
            return content;
        string projectTag = content.Substring(startIndex, endIndex - startIndex + 1);
        return content.Replace(projectTag, "").Replace("</Project>", "");
    }
}
