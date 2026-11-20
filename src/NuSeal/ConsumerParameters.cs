using System.Collections.Generic;
using System.Linq;

namespace NuSeal;

internal class ConsumerParameters
{
    public ConsumerParameters(
        string nuSealAssetsPath,
        string nuSealVersion,
        string outputPath,
        string packageId,
        string assemblyName,
        PemData[] pems,
        string? condition,
        string? propsFile,
        string? targetsFile,
        string? validationMode,
        string? validationScope)
    {
        NuSealAssetsPath = nuSealAssetsPath;
        NuSealVersion = nuSealVersion;
        OutputPath = outputPath;
        PackageId = packageId;
        AssemblyName = assemblyName;
        Pems = pems;
        PropsFile = string.IsNullOrWhiteSpace(propsFile) ? null : propsFile;
        TargetsFile = string.IsNullOrWhiteSpace(targetsFile) ? null : targetsFile;

        Options = new NuSealOptions(validationMode, validationScope);
        ValidationMode = Options.ValidationMode.ToString();
        ValidationScope = Options.ValidationScope.ToString();
        PackageSuffix = packageId.Replace(".", "");
        NuSealVersionSuffix = nuSealVersion.Replace(".", "_").Replace("-", "_");

        string? conditionExpression;
        if (string.IsNullOrWhiteSpace(condition))
        {
            conditionExpression = Options.ValidationScope == NuSealValidationScope.Transitive
                ? "'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'"
                : null;
        }
        else
        {
            var parsedCondition = condition!.Replace("##", "@").Replace("#", "$");
            conditionExpression = parsedCondition.Length >= 2 && parsedCondition[0] == '"' && parsedCondition[parsedCondition.Length - 1] == '"'
                ? parsedCondition.Substring(1, parsedCondition.Length - 2)
                : parsedCondition;
        }

        var licenseConditions = pems
            .SelectMany(pem => pem.AllowedLicenses.Select(license => $"'$({pem.ProductName}License)' != '{license}'"))
            .ToArray();

        var conditions = new List<string>();
        if (conditionExpression is not null)
        {
            conditions.Add(licenseConditions.Length > 0 ? $"({conditionExpression})" : conditionExpression);
        }
        conditions.AddRange(licenseConditions);

        TargetCondition = conditions.Count > 0
            ? $"Condition=\"{string.Join(" And ", conditions)}\""
            : "";
    }

    public string NuSealAssetsPath { get; }
    public string NuSealVersion { get; }
    public string OutputPath { get; }
    public string PackageId { get; }
    public string AssemblyName { get; }
    public PemData[] Pems { get; }
    public string TargetCondition { get; }
    public string? PropsFile { get; }
    public string? TargetsFile { get; }
    public string ValidationMode { get; }
    public string ValidationScope { get; }
    public string PackageSuffix { get; }
    public string NuSealVersionSuffix { get; }
    public NuSealOptions Options { get; }
}
