using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NuSeal;

public class LicenseValidationTask : Task
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

            var dllFiles = GetDllFiles(outputDirectory, MainAssemblyPath);
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

                    var pemConfigs = ExtractPemFromAssembly(assembly);

                    if (pemConfigs.Count == 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dllFile);
                        Log.LogWarning($"NuSeal: No public key resources found for {fileName}. Path: {dllFile}.");
                        return true;
                    }

                    var hasValidLicense = pemConfigs.Any(config =>
                        TryGetLicenseContent(MainAssemblyPath, config.ProductName, out var licenseContent)
                        && LicenseValidator.IsValid(config.PublicKeyPem, licenseContent, config.ProductName));

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

    private static string[] GetDllFiles(string directory, string mainAssemblyPath)
    {
        var dllFiles = Directory.GetFiles(directory, "*.dll")
            .Where(x =>
            {
                var fileName = Path.GetFileName(x);
                return
                    !x.Equals(mainAssemblyPath, StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("NuSeal", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("Windows", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        return dllFiles;
    }

    private static readonly char[] _resourceNameDelimiter = new[] { '.' };
    private static List<PemConfig> ExtractPemFromAssembly(AssemblyDefinition assembly)
    {
        var pemConfigs = new List<PemConfig>();

        if (assembly.MainModule.HasResources is false)
        {
            return pemConfigs;
        }

        const string pemFileSuffix = "nuseal.pem";

        foreach (var resource in assembly.MainModule.Resources)
        {
            if (resource is EmbeddedResource embeddedResource
                && embeddedResource.Name.EndsWith(pemFileSuffix, StringComparison.OrdinalIgnoreCase))
            {
                using var stream = embeddedResource.GetResourceStream();
                using var reader = new StreamReader(stream);
                var pemContent = reader.ReadToEnd();

                var parts = embeddedResource.Name.Split(_resourceNameDelimiter, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    continue;
                }

                var productName = parts[parts.Length - 3]; // Get the part before "nuseal.pem"
                var pemConfig = new PemConfig(productName, pemContent);
                pemConfigs.Add(pemConfig);
            }
        }

        return pemConfigs;
    }

    private static bool TryGetLicenseContent(string mainAssemblyPath, string productName, out string licenseContent)
    {
        var licenseFileName = $"{productName}.license";

        try
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(mainAssemblyPath)!);
            while (dir is not null)
            {
                var file = Path.Combine(dir.FullName, licenseFileName);
                if (File.Exists(file))
                {
                    licenseContent = File.ReadAllText(file).Trim();
                    return true;
                }
                dir = dir.Parent;
            }
        }
        catch
        {
        }

        licenseContent = string.Empty;
        return false;
    }

    private class PemConfig
    {
        public PemConfig(string productName, string publicKeyPem)
        {
            ProductName = productName;
            PublicKeyPem = publicKeyPem;
        }

        public string ProductName { get; }
        public string PublicKeyPem { get; }
    }
}
