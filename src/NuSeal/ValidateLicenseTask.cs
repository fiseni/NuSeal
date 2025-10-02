using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Linq;

namespace NuSeal;

public partial class ValidateLicenseTask : Task
{
    public ITaskItem[] PackageReferences { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] ResolvedCompileFileDefinitions { get; set; } = Array.Empty<ITaskItem>();
    public string MainAssemblyPath { get; set; } = "";
    public string ProtectedPackageId { get; set; } = "";
    public string ProtectedAssemblyName { get; set; } = "";
    public string ValidationMode { get; set; } = "";
    public string ValidationScope { get; set; } = "";

    public override bool Execute()
    {
        if (string.IsNullOrWhiteSpace(MainAssemblyPath)
            || string.IsNullOrWhiteSpace(ProtectedPackageId)
            || string.IsNullOrWhiteSpace(ProtectedAssemblyName)
            || string.IsNullOrWhiteSpace(ValidationMode)
            || string.IsNullOrWhiteSpace(ValidationScope))
        {
            // This should never happen as we always pass all arguments while preparing assets for authors.
            // If that's the case, something went really wrong, and we won't break end users' builds.
            Log.LogMessage(MessageImportance.High, "NuSeal: Invalid arguments for {0}", nameof(ValidateLicenseTask));
            return true;
        }

        var options = new NuSealOptions(ValidationMode, ValidationScope);

        if (!TryGetProtectedDllPath(options, out var protectedDll))
        {
            // If this task is being executed, it must have come from a protected package.
            // Something went wrong if we can't find the protected dll.
            // But we won't break end users' builds.
            Log.LogMessage(MessageImportance.High, "NuSeal: No protected DLL was found for NuGet Package: {0}", ProtectedPackageId);
            return true;
        }

        try
        {
            using var assembly = AssemblyDefinition.ReadAssembly(protectedDll);
            var pems = AssemblyUtils.ExtractPems(assembly);
            if (pems.Count == 0)
            {
                Log.LogMessage(MessageImportance.High, "NuSeal: No public key resources found for NuGet Package: {0}.", ProtectedPackageId);
                return true;
            }

            var bestValidationResult = LicenseValidationResult.Invalid;

            foreach (var pem in pems)
            {
                if (FileUtils.TryGetLicense(MainAssemblyPath, pem.ProductName, out var license))
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
            Log.LogMessage(MessageImportance.High, "NuSeal: Failed to process {0}. Error: {1}", protectedDll, ex.Message);
        }

        return true;
    }

    private bool TryGetProtectedDllPath(
        NuSealOptions options,
        out string dllPath)
    {
        if (options.ValidationScope == NuSealValidationScope.Direct
            && !PackageReferences.Any(x => string.Equals(x.ItemSpec, ProtectedPackageId, StringComparison.OrdinalIgnoreCase)))
        {
            dllPath = "";
            return false;
        }

        foreach (var file in ResolvedCompileFileDefinitions)
        {
            var nugetPackageId = file.GetMetadata("NuGetPackageId");
            if (string.Equals(nugetPackageId, ProtectedPackageId, StringComparison.OrdinalIgnoreCase))
            {
                dllPath = file.ItemSpec;
                return true;
            }

        }

        dllPath = "";
        return false;
    }
}
