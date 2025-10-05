using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Linq;

namespace NuSeal;

public partial class ValidateLicenseTask_0_4_0 : Task
{
    public string TargetAssemblyPath { get; set; } = "";
    public string NuSealVersion { get; set; } = "";
    public string ProtectedPackageId { get; set; } = "";
    public string ProtectedAssemblyName { get; set; } = "";
    public ITaskItem[] Pems { get; set; } = Array.Empty<ITaskItem>();
    public string ValidationMode { get; set; } = "";
    public string ValidationScope { get; set; } = "";

    public override bool Execute()
    {
        if (string.IsNullOrWhiteSpace(TargetAssemblyPath)
            || string.IsNullOrWhiteSpace(NuSealVersion)
            || string.IsNullOrWhiteSpace(ProtectedPackageId)
            || string.IsNullOrWhiteSpace(ProtectedAssemblyName)
            || Pems is null
            || Pems.Length == 0
            || string.IsNullOrWhiteSpace(ValidationMode)
            || string.IsNullOrWhiteSpace(ValidationScope))
        {
            // This should never happen as we always pass all arguments while preparing assets for authors.
            // If that's the case, something went really wrong, and we won't break end users' builds.
            Log.LogMessage(MessageImportance.High, "NuSeal: Invalid arguments for {0}", nameof(ValidateLicenseTask_0_4_0));
            return true;
        }

        var options = new NuSealOptions(ValidationMode, ValidationScope);

        try
        {
            var pems = Pems.Select(x =>
            {
                var publicKeyPem = x.ItemSpec.Trim();
                var productName = x.GetMetadata("ProductName");
                return new PemData(productName, publicKeyPem);
            });

            var bestValidationResult = LicenseValidationResult.Invalid;

            foreach (var pem in pems)
            {
                if (FileUtils.TryGetLicense(TargetAssemblyPath, pem.ProductName, out var license))
                {
                    var validationResult = LicenseValidator.Validate(pem, license);
                    if (validationResult == LicenseValidationResult.Valid)
                    {
                        return true;
                    }

                    if (validationResult < bestValidationResult)
                    {
                        bestValidationResult = validationResult;
                    }
                }
            }

            var errorMessage = bestValidationResult switch
            {
                LicenseValidationResult.ExpiredWithinGracePeriod
                    => "NuSeal: License for {0} has expired but is within the grace period. Please renew your license soon.",
                LicenseValidationResult.ExpiredOutsideGracePeriod
                    => "NuSeal: License for {0} has expired. Please renew your license.",
                _ => "NuSeal: No valid license found for NuGet Package: {0}."
            };

            if (bestValidationResult == LicenseValidationResult.ExpiredWithinGracePeriod)
            {
                Log.LogWarning(errorMessage, ProtectedPackageId);
                return true;
            }

            if (options.ValidationMode == NuSealValidationMode.Warning)
            {
                Log.LogWarning(errorMessage, ProtectedPackageId);
                return true;
            }
            else
            {
                Log.LogError(errorMessage, ProtectedPackageId);
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.High, "NuSeal: Failed to process license validation for {0}. Error: {1}", ProtectedPackageId, ex.Message);
        }

        return true;
    }
}
