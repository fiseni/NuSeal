using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

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

    public string ValidationScope { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            return PrepareAssetsForConsumer.Execute(
                NuSealAssetsPath,
                NuSealVersion,
                OutputPath,
                ConsumerPackageId,
                ConsumerPropsFile,
                ConsumerTargetsFile,
                ValidationScope);

        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
