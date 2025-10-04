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
        Suffix = packageId.Replace(".", "");

        if (string.IsNullOrWhiteSpace(condition))
        {
            TargetCondition = Options.ValidationScope == NuSealValidationScope.Transitive
                ? $"Condition=\"'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'\""
                : "";
        }
        else
        {
            var parsedCondition = condition!.Replace("##", "$").Replace("#", "$");
            if (parsedCondition[0] != '"')
            {
                parsedCondition = $"\"{parsedCondition}";
            }
            if (parsedCondition[parsedCondition.Length - 1] != '"')
            {
                parsedCondition = $"{parsedCondition}\"";
            }
            TargetCondition = $"Condition={parsedCondition}";
        }
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
    public string Suffix { get; set; }
    public NuSealOptions Options { get; }
}
