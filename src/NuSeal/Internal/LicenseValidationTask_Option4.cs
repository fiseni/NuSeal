using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace NuSeal.Internal;

// Parsing the assembly files as text to extract the embedded public key PEM and product name
// Very simplistic and rudimentary approach. It works for simple scenarios with a single pem, but I'm not confident.
[ExcludeFromCodeCoverage]
internal class LicenseValidationTask_Option4 : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        //Debugger.Launch();

        try
        {
            var dllFiles = GetDllFiles();
            var pemConfigs = GetPemConfigs(dllFiles);

            if (pemConfigs.Length == 0)
            {
                Log.LogWarning("NuSeal: No public key resources found in any NuSeal protected assemblies.");
                return true;
            }

            foreach (var config in pemConfigs)
            {
                if (!TryGetLicenseContent(config.ProductName, out var licenseContent))
                {
                    Log.LogError($"NuSeal: License file for '{config.ProductName}' not found.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(licenseContent))
                {
                    Log.LogError($"NuSeal: License file for '{config.ProductName}' is empty.");
                    return false;
                }

                if (!LicenseValidator.IsValid(config.PublicKeyPem, licenseContent, config.ProductName))
                {
                    Log.LogError($"NuSeal: License file for '{config.ProductName}' is invalid.");
                    return false;
                }

                Log.LogMessage(MessageImportance.High, $"NuSeal: License for '{config.ProductName}' is valid.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private string[] GetDllFiles()
    {
        var outputDirectory = Path.GetDirectoryName(MainAssemblyPath);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            Log.LogWarning($"NuSeal: Cannot determine output directory from MainAssemblyPath: {MainAssemblyPath}");
            return Array.Empty<string>();
        }

        var dllFiles = Directory.GetFiles(outputDirectory, "*.dll")
            .Where(x =>
            {
                var fileName = Path.GetFileName(x);
                return
                    !x.Equals(MainAssemblyPath, StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("NuSeal", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.StartsWith("Windows", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        return dllFiles;
    }

    private PemConfig[] GetPemConfigs(string[] dllFiles)
    {
        if (dllFiles.Length == 0)
        {
            return Array.Empty<PemConfig>();
        }

        var pemConfigs = new List<PemConfig>();

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var fileContent = File.ReadAllText(dllFile);
                if (!fileContent.Contains("NuSealProtectedAttribute"))
                {
                    continue;
                }

                var pemConfig = ExtractPemConfig(fileContent);
                if (pemConfig is not null)
                {
                    pemConfigs.Add(pemConfig);
                }
            }
            catch (Exception ex)
            {
                Log.LogWarningFromException(ex, true);
            }
        }

        var uniquePemConfigs = pemConfigs
            .GroupBy(x => x.ProductName)
            .Select(g => g.First())
            .ToArray();

        return uniquePemConfigs;
    }

    private static PemConfig? ExtractPemConfig(string fileContent)
    {
        const string startToken = "-----BEGIN";
        const string endToken = "KEY-----";

        var startIndex = fileContent.IndexOf(startToken, 0);
        if (startIndex == -1) return null;
        var endIndex = fileContent.IndexOf(endToken, startIndex);
        if (endIndex == -1) return null;
        endIndex += endToken.Length;
        endIndex = fileContent.IndexOf(endToken, endIndex);
        if (endIndex == -1) return null;
        endIndex += endToken.Length;

        var pemContent = fileContent.Substring(startIndex, endIndex - startIndex);
        var resourceNameIndex = fileContent.LastIndexOf(".nuseal.pem", startIndex);
        var dotIndex = fileContent.LastIndexOf('.', resourceNameIndex - 1);
        var productName = fileContent.Substring(dotIndex + 1, resourceNameIndex - dotIndex - 1).Trim();

        return new PemConfig(productName, pemContent);
    }

    private bool TryGetLicenseContent(string productName, out string licenseContent)
    {
        var licenseFileName = $"{productName}.license";

        try
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(MainAssemblyPath)!);
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
        catch (Exception ex)
        {
            Log.LogWarningFromException(ex, true);
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
