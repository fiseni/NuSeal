using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.IO;
using System.Linq;

namespace NuSeal;

internal class LicenseValidation
{
    public static bool Execute(
        TaskLoggingHelper log,
        string mainAssemblyPath,
        string[] dllFiles,
        NuSealValidationScope validationScope)
    {
        try
        {
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    using var assembly = AssemblyDefinition.ReadAssembly(dllFile);
                    var options = AssemblyUtils.ExtractOptions(assembly);

                    if (options.IsProtected is false)
                        continue;

                    if (validationScope != options.ValidationScope)
                        continue;

                    var pems = AssemblyUtils.ExtractPems(assembly);

                    if (pems.Count == 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        log.LogMessage(MessageImportance.High, "NuSeal: No public key resources found for {0}. Path: {1}.", fileName, dllFile);
                        return true;
                    }

                    var hasValidLicense = pems.Any(pem =>
                        FileUtils.TryGetLicense(mainAssemblyPath, pem.ProductName, out var license)
                        && LicenseValidator.IsValid(pem, license));

                    if (hasValidLicense is false)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);

                        if (options.ValidationMode == NuSealValidationMode.Warning)
                        {
                            log.LogWarning("NuSeal: No valid license found for {0}. Path: {1}.", fileName, dllFile);
                        }
                        else
                        {
                            log.LogError("NuSeal: No valid license found for {0}. Path: {1}.", fileName, dllFile);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogMessage(MessageImportance.High, "NuSeal: Failed to process {0}. Error: {1}", dllFile, ex.Message);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            log.LogMessage(MessageImportance.High, "NuSeal: Failed to process. Error: {0}", ex.Message);
            return true;
        }
    }
}
