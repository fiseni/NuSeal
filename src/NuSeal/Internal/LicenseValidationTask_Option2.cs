using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace NuSeal.Internal;

// I explored the idea of package authors passing the public key directly via MSBuild properties.
// But it seems too cumbersome to manage long PEM strings in project files.
// Also, it would require authors to add and pack targets in buildTransitive which may/may not be confusing.
[ExcludeFromCodeCoverage]
internal class LicenseValidationTask_Option2 : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public string[] NuSealConfigs { get; set; } = [];

    public override bool Execute()
    {
        //Debugger.Launch();

        PrintState(NuSealConfigs);

        var configs = SanitizeConfiguration(NuSealConfigs);

        PrintConfig(configs);

        if (configs.Length == 0)
        {
            Log.LogWarning("No license public keys provided.");
            return true;
        }

        foreach (var config in configs)
        {
            if (TryGetLicenseContent(config.ProductName, out var content))
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log.LogError($"License file for '{config.ProductName}' is empty.");
                    return false;
                }

                if (!LicenseValidator.IsValid(config.PublicKeyPem, content, config.ProductName))
                {
                    Log.LogError($"License file for '{config.ProductName}' is invalid.");
                    return false;
                }

                Log.LogMessage(MessageImportance.High, $"License for '{config.ProductName}' is valid.");
            }
            else
            {
                Log.LogError($"License file for '{config.ProductName}' not found.");
                return false;
            }
        }

        return true;
    }

    // For debugging purposes
    private void PrintState(string[] configs)
    {
        Log.LogMessage(MessageImportance.High, $"MainAssemblyPath: {MainAssemblyPath}");
        foreach (var config in configs)
        {
            Log.LogMessage(MessageImportance.High, $"Config: {config}");
        }
    }

    // For debugging purposes
    private void PrintConfig(Config[] configs)
    {
        foreach (var config in configs)
        {
            Log.LogMessage(MessageImportance.High, $"Product Name: {config.ProductName}, Public Key: {config.PublicKeyPem}");
        }
    }

    private static readonly char[] _lf = new[] { '\n' };
    private static readonly char[] _delimiter = new[] { ',' };

    private static Config[] SanitizeConfiguration(string[] configs)
    {
        return configs
            .Select(x => x.Split(_delimiter, 2, StringSplitOptions.RemoveEmptyEntries))
            .Where(x => x.Length == 2)
            .Select(x => new Config(x[0].Trim(), SanitizePem(x[1])))
            .GroupBy(x => x.ProductName)
            .Select(x => x.Last())
            .ToArray();

        static string SanitizePem(string pem)
        {
            var lines = pem
                .Split(_lf, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            return string.Join("\r\n", lines);
        }
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

    private class Config(string productName, string publicKeyPem)
    {
        public string ProductName { get; } = productName;
        public string PublicKeyPem { get; } = publicKeyPem;
    }
}
