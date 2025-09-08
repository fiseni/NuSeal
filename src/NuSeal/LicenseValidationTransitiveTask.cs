using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NuSeal;

public partial class LicenseValidationTransitiveTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        return LicenseValidation.Execute(Log, MainAssemblyPath, NuSealValidationScope.Transitive);
    }
}
