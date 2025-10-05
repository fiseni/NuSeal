using System;
using System.IO;
using System.Linq;

namespace NuSeal;

internal class Assets
{
    private const string TASK_NAME = nameof(ValidateLicenseTask_0_4_0);

    public static string GenerateProps(ConsumerParameters parameters)
    {
        var pemItems = string.Join(Environment.NewLine, parameters.Pems.Select(x =>
        {
            var item = $"""
            <Pem_{parameters.PackageSuffix} Include="
            {x.PublicKeyPem}
            " ProductName="{x.ProductName}" />
            """;
            return item;
        }));

        var output = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <PropertyGroup>
                <NuSealAssembly_{parameters.NuSealVersionSuffix}>$([MSBuild]::NormalizePath('$(NugetPackageRoot)', 'nuseal', '{parameters.NuSealVersion}', 'tasks', 'netstandard2.0', 'NuSeal_{parameters.NuSealVersionSuffix}.dll'))</NuSealAssembly_{parameters.NuSealVersionSuffix}>
              </PropertyGroup>

              <ItemGroup>
            {pemItems}
              </ItemGroup>

              <UsingTask
                TaskName="NuSeal.{TASK_NAME}"
                AssemblyFile="$(NuSealAssembly_{parameters.NuSealVersionSuffix})"
                TaskFactory="TaskHostFactory" />

            </Project>
            """;
        return output;
    }

    public static string GenerateTargets(ConsumerParameters parameters)
    {
        var output = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_{parameters.PackageSuffix}"
                      AfterTargets="AfterBuild"
                      {parameters.TargetCondition}>

                <NuSeal.{TASK_NAME}
                  TargetAssemblyPath="$(TargetPath)"
                  NuSealVersion="{parameters.NuSealVersion}"
                  ProtectedPackageId="{parameters.PackageId}"
                  ProtectedAssemblyName="{parameters.AssemblyName}"
                  Pems="@(Pem_{parameters.PackageSuffix})"
                  ValidationMode="{parameters.ValidationMode}"
                  ValidationScope="{parameters.ValidationScope}"
                  />

              </Target>

            </Project>
            """;
        return output;
    }

    public static string AppendConsumerAsset(string nuSealAssetContent, string? consumerAssetFile)
    {
        if (consumerAssetFile is not null && File.Exists(consumerAssetFile))
        {
            var content = File.ReadAllText(consumerAssetFile);
            content = RemoveProjectTags(content, consumerAssetFile);
            if (!string.IsNullOrWhiteSpace(content))
            {
                var linedEnding = DetectLineEnding(content);
                nuSealAssetContent = nuSealAssetContent.Replace("</Project>", $"{content}{linedEnding}</Project>");
            }
        }
        return nuSealAssetContent;
    }

    // Very rudimentary, but it's not worth parsing the XML properly for this
    public static string RemoveProjectTags(string content, string fileName)
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

    public static string DetectLineEnding(string content)
    {
        var index = content.IndexOf('\n');
        if (index > 0 && content[index - 1] == '\r')
            return "\r\n";
        return "\n";
    }
}
