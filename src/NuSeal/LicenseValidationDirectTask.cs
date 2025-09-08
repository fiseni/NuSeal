using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Linq;

namespace NuSeal;

public partial class LicenseValidationDirectTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    [Required]
    public ITaskItem[] PackageReferences { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public ITaskItem[] ResolvedCompileFileDefinitions { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var directPackageIds = PackageReferences.Select(x => x.ItemSpec).ToArray();

        var directPackageDlls = ResolvedCompileFileDefinitions
            .Where(x => directPackageIds.Contains(x.GetMetadata("NuGetPackageId")))
            .Select(x => x.ItemSpec)
            .ToArray();

        return LicenseValidation.Execute(Log, MainAssemblyPath, directPackageDlls, NuSealValidationScope.Direct);
    }
}
