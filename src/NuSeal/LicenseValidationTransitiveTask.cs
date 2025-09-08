using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Linq;

namespace NuSeal;

public partial class LicenseValidationTransitiveTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    [Required]
    public ITaskItem[] PackageReferences { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public ITaskItem[] ResolvedCompileFileDefinitions { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var packageDlls = ResolvedCompileFileDefinitions
            .Select(x => x.ItemSpec)
            .ToArray();

        return LicenseValidation.Execute(Log, MainAssemblyPath, packageDlls, NuSealValidationScope.Transitive);
    }
}
