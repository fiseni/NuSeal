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
                Log.LogWarning($"NuSeal: Cannot determine output directory from MainAssemblyPath: {MainAssemblyPath}");
                return true;
            }

            var dllFiles = Utils.GetDllFiles(outputDirectory, MainAssemblyPath);
            if (dllFiles.Length == 0)
            {
                Log.LogWarning("NuSeal: No applicable dll files found in the output directory.");
                return true;
            }

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(dllFile);

                    var hasNuSealAttribute = assembly.CustomAttributes
                        .Any(attr => attr.AttributeType.FullName == typeof(NuSealProtectedAttribute).FullName);

                    if (hasNuSealAttribute is false)
                        continue;

                    var pems = Utils.ExtractPemFromAssembly(assembly);

                    if (pems.Count == 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        Log.LogWarning($"NuSeal: No public key resources found for {fileName}. Path: {dllFile}.");
                        return true;
                    }

                    var hasValidLicense = pems.Any(pem =>
                        Utils.TryGetLicenseContent(MainAssemblyPath, pem.ProductName, out var licenseContent)
                        && LicenseValidator.IsValid(pem, licenseContent));

                    if (hasValidLicense is false)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        Log.LogError($"NuSeal: No valid license found for {fileName}. Path: {dllFile}.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"NuSeal: Failed to process {dllFile}. Error: {ex.Message}");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogWarningFromException(ex, true);
            return true;
        }
    }
}
