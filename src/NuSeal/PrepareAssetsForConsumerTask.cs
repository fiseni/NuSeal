using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace NuSeal;

public partial class PrepareAssetsForConsumerTask : Task
{
    [Required]
    public string NuSealAssetsPath { get; set; } = "";

    [Required]
    public string NuSealVersion { get; set; } = "";

    [Required]
    public string OutputPath { get; set; } = "";

    [Required]
    public string ConsumerPackageId { get; set; } = "";

    public string ConsumerPropsFile { get; set; } = "";

    public string ConsumerTargetsFile { get; set; } = "";

    public override bool Execute()
    {
        //Debugger.Launch();

        try
        {
            var nusealPropsFile = Path.Combine(NuSealAssetsPath, "NuSeal.Direct.props");
            var nusealTargetsFile = Path.Combine(NuSealAssetsPath, "NuSeal.Direct.targets");
            var propsOutputFile = Path.Combine(OutputPath, $"{ConsumerPackageId}.props");
            var targetsOutputFile = Path.Combine(OutputPath, $"{ConsumerPackageId}.targets");

            var props = File.ReadAllText(nusealPropsFile);
            var targets = File.ReadAllText(nusealTargetsFile);

            props = props.Replace("_NuSealVersion_", NuSealVersion);

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
