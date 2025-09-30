namespace NuSeal;

internal class ConsumerParameters
{
    public ConsumerParameters(
        string nuSealAssetsPath,
        string nuSealVersion,
        string outputPath,
        string packageId,
        string assemblyName,
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
        PropsFile = string.IsNullOrWhiteSpace(propsFile) ? null : propsFile;
        TargetsFile = string.IsNullOrWhiteSpace(targetsFile) ? null : targetsFile;

        Options = new NuSealOptions(validationMode, validationScope);
        ValidationMode = Options.ValidationMode.ToString();
        ValidationScope = Options.ValidationScope.ToString();
        Suffix = packageId.Replace(".", "");
    }

    public string NuSealAssetsPath { get; }
    public string NuSealVersion { get; }
    public string OutputPath { get; }
    public string PackageId { get; }
    public string AssemblyName { get; }
    public string? PropsFile { get; }
    public string? TargetsFile { get; }
    public string ValidationMode { get; }
    public string ValidationScope { get; }
    public string Suffix { get; set; }
    public NuSealOptions Options { get; }
}
