using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NuSeal;

public partial class LicenseValidationTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        //Debugger.Launch();

        try
        {
            var outputDirectory = Path.GetDirectoryName(MainAssemblyPath);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                Log.LogMessage(MessageImportance.High, "NuSeal: Cannot determine output directory from MainAssemblyPath: {0}", MainAssemblyPath);
                return true;
            }

            var dllFiles = FileUtils.GetDllFiles(outputDirectory, MainAssemblyPath);
            if (dllFiles.Length == 0)
            {
                Log.LogMessage(MessageImportance.High, "NuSeal: No applicable dll files found in the output directory.");
                return true;
            }

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(dllFile);

                    if (AssemblyUtils.IsNuSealProtected(assembly) is false)
                        continue;

                    var options = AssemblyUtils.ExtractOptions(assembly);
                    var pems = AssemblyUtils.ExtractPems(assembly);

                    if (pems.Count == 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        Log.LogMessage(MessageImportance.High, "NuSeal: No public key resources found for {0}. Path: {1}.", fileName, dllFile);
                        return true;
                    }

                    var hasValidLicense = pems.Any(pem =>
                        FileUtils.TryGetLicense(MainAssemblyPath, pem.ProductName, out var license)
                        && LicenseValidator.IsValid(pem, license));

                    if (hasValidLicense is false)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);

                        if (options.ValidationBehavior.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                        {
                            Log.LogWarning("NuSeal: No valid license found for {0}. Path: {1}.", fileName, dllFile);
                        }
                        else
                        {
                            Log.LogError("NuSeal: No valid license found for {0}. Path: {1}.", fileName, dllFile);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogMessage(MessageImportance.High, "NuSeal: Failed to process {0}. Error: {1}", dllFile, ex.Message);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.High, "NuSeal: Failed to process. Error: {0}", ex.Message);
            return true;
        }
    }
}
