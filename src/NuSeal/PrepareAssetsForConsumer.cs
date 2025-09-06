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
            content = RemoveProjectTags(content, consumerPropsFile);
            props = props.Replace("</Project>", $"{content}{Environment.NewLine}</Project>");
        }

        if (!string.IsNullOrEmpty(consumerTargetsFile) && File.Exists(consumerTargetsFile))
        {
            var content = File.ReadAllText(consumerTargetsFile);
            content = RemoveProjectTags(content, consumerTargetsFile);
            targets = targets.Replace("</Project>", $"{content}{Environment.NewLine}</Project>");
        }

        File.WriteAllText(propsOutputFile, props);
        File.WriteAllText(targetsOutputFile, targets);

        return true;
    }

    // Very rudimentary, but it's not worth parsing the XML properly for this
    private static string RemoveProjectTags(string content, string fileName)
    {
        var startIndex = content.IndexOf("<Project");
        if (startIndex == -1)
            throw new ArgumentException($"The provided content does not contain a <Project> tag. File {fileName}");

        var endIndex = content.IndexOf(">", startIndex);
        if (endIndex == -1)
            throw new ArgumentException($"The provided content has invalid xml content. File {fileName}");

        var closingTagIndex = content.IndexOf("</Project>", endIndex);
        if (closingTagIndex == -1)
            throw new ArgumentException($"The provided content does not contain a closing </Project> tag. File {fileName}");

        string projectTag = content.Substring(startIndex, endIndex - startIndex + 1);
        return content.Replace(projectTag, "").Replace("</Project>", "");
    }
}
