using System;
using System.Diagnostics;
using System.IO;

namespace NuSeal;

internal class PrepareAssetsForConsumer
{
    public static bool Execute(
        string nusealAssetsPath,
        string nusealVersion,
        string outputPath,
        string consumerPackageId,
        string consumerPropsFile,
        string consumerTargetsFile)
    {
        //Debugger.Launch();

        var nusealPropsFile = Path.Combine(nusealAssetsPath, "NuSeal.Direct.props");
        var nusealTargetsFile = Path.Combine(nusealAssetsPath, "NuSeal.Direct.targets");
        var propsOutputFile = Path.Combine(outputPath, $"{consumerPackageId}.props");
        var targetsOutputFile = Path.Combine(outputPath, $"{consumerPackageId}.targets");

        var props = File.ReadAllText(nusealPropsFile);
        var targets = File.ReadAllText(nusealTargetsFile);

        props = props.Replace("_NuSealVersion_", nusealVersion);

        if (!string.IsNullOrEmpty(consumerPropsFile) && File.Exists(consumerPropsFile))
        {
            var content = File.ReadAllText(consumerPropsFile);
            content = RemoveProjectTags(content);
            props = props.Replace("</Project>", $"{content}{Environment.NewLine}</Project>");
        }

        if (!string.IsNullOrEmpty(consumerTargetsFile) && File.Exists(consumerTargetsFile))
        {
            var content = File.ReadAllText(consumerTargetsFile);
            content = RemoveProjectTags(content);
            targets = targets.Replace("</Project>", $"{content}{Environment.NewLine}</Project>");
        }

        File.WriteAllText(propsOutputFile, props);
        File.WriteAllText(targetsOutputFile, targets);

        return true;
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
