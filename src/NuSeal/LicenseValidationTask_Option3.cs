using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuSeal;

// This was the most elegant idea.
// The package authors just add the pem file and include it as an embedded resource.
// However, the MSBuild task does not run on .NET, and is unable to load .NET assemblies.
public class LicenseValidationTask_Option3 : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        Debugger.Launch();

        try
        {
            var dllFiles = GetDllFiles();
            var pemConfigs = GetPemConfigs(dllFiles);
            if (pemConfigs.Length == 0)
            {
                Log.LogWarning("NuSeal: No public key PEM resources found in any NuSeal protected assemblies.");
                return true;
            }

            foreach (var config in pemConfigs)
            {
                if (TryGetLicenseContent(config.ProductName, out var licenseContent))
                {
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
                else
                {
                    Log.LogError($"NuSeal: License file for '{config.ProductName}' not found.");
                    return false;
                }
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

        var allDllFiles = Directory.GetFiles(outputDirectory, "*.dll");

        var dllFiles = allDllFiles
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

        Log.LogMessage(MessageImportance.High, $"NuSeal: Found {dllFiles.Length} DLL files to scan for NuSeal protected packages.");

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
                if (!IsLikelyManagedAssembly(dllFile))
                {
                    continue;
                }

                var assembly = Assembly.LoadFrom(dllFile);

                if (!assembly.GetCustomAttributes<NuSealProtectedAttribute>().Any())
                {
                    continue;
                }

                var resourceNames = assembly.GetManifestResourceNames();
                var pemResources = resourceNames
                    .Where(r => r.EndsWith("nuseal.pem", StringComparison.OrdinalIgnoreCase));

                foreach (var pemResource in pemResources)
                {
                    try
                    {
                        if (!TryExtractProductNameFromResourceName(pemResource, out var productName))
                        {
                            Log.LogWarning($"NuSeal: Unable to extract product name from resource '{pemResource}' in {Path.GetFileName(dllFile)}");
                            continue;
                        }

                        // Read the PEM content
                        using var stream = assembly.GetManifestResourceStream(pemResource);
                        if (stream is null) continue;

                        using var reader = new StreamReader(stream);
                        var pemContent = reader.ReadToEnd();

                        if (!string.IsNullOrWhiteSpace(pemContent))
                        {
                            pemConfigs.Add(new PemConfig(productName, pemContent));
                            Log.LogMessage(MessageImportance.High, $"NuSeal: Found public key PEM for product '{productName}' in {Path.GetFileName(dllFile)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"NuSeal: Failed to extract PEM content from resource '{pemResource}' in {Path.GetFileName(dllFile)}: {ex.Message}");
                    }
                }
            }
            catch (BadImageFormatException)
            {
                // Skip non-managed assemblies silently
            }
            catch (Exception ex)
            {
                Log.LogWarning($"NuSeal: Failed to load assembly {Path.GetFileName(dllFile)}: {ex.Message}");
            }
        }

        var uniquePemConfigs = pemConfigs
            .GroupBy(x => x.ProductName)
            .Select(g => g.Last())
            .ToArray();

        return uniquePemConfigs;
    }

    private static bool IsLikelyManagedAssembly(string filePath)
    {
        try
        {
            // A quick check to see if this is likely a managed assembly
            // by checking for PE header and basic characteristics
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            if (fileStream.Length < 64)  // Too small to be a valid PE file
                return false;

            using var reader = new BinaryReader(fileStream);

            // Check DOS header magic number "MZ"
            if (reader.ReadInt16() != 0x5A4D)
                return false;

            // Find the PE header offset
            fileStream.Position = 0x3C;
            var peHeaderOffset = reader.ReadInt32();

            // Ensure PE header offset is within file bounds
            if (peHeaderOffset < 0 || peHeaderOffset > fileStream.Length - 4)
                return false;

            // Check PE header signature "PE\0\0"
            fileStream.Position = peHeaderOffset;
            return reader.ReadInt32() == 0x00004550;
        }
        catch
        {
            return false;
        }
    }

    private static readonly char[] _delimiter = new[] { '.' };

    private static bool TryExtractProductNameFromResourceName(string resourceName, out string productName)
    {
        var parts = resourceName.Split(_delimiter, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            productName = string.Empty;
            return false;
        }

        productName = parts[parts.Length - 3]; // Get the part before "nuseal.pem"
        return true;
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
                    licenseContent = File.ReadAllText(file);
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
