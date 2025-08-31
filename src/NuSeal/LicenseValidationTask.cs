using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace NuSeal;

public class LicenseValidationTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public string[] LicensePublicKeys { get; set; } = [];

    public override bool Execute()
    {
        PrintState();

        var keys = NormalizePublicKeys(LicensePublicKeys);

        PrintNormalizedKeys(keys);

        if (keys.Length == 0)
        {
            Log.LogWarning("No license public keys provided.");
            return true;
        }

        foreach (var key in keys)
        {
            if (TryGetLicenseContent(key.Name, out var content))
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log.LogError($"License file for '{key.Name}' is empty.");
                    return false;
                }

                if (!LicenseValidator.IsValid(content, key.Value))
                {
                    Log.LogError($"License file for '{key.Name}' is invalid.");
                    return false;
                }

                Log.LogMessage(MessageImportance.High, $"License for '{key.Name}' is valid.");
            }
            else
            {
                Log.LogError($"License file for '{key.Name}' not found.");
                return false;
            }
        }

        return true;
    }

    // For debugging purposes
    private void PrintState()
    {
        Log.LogMessage(MessageImportance.High, $"MainAssemblyPath: {MainAssemblyPath}");
        foreach (var key in LicensePublicKeys)
        {
            Log.LogMessage(MessageImportance.High, $"PublicKey: {key}");
        }
    }

    // For debugging purposes
    private void PrintNormalizedKeys(KeyInfo[] keys)
    {
        foreach (var key in keys)
        {
            Log.LogMessage(MessageImportance.High, $"Product Name: {key.Name}, Public Key: {key.Value}");
        }
    }

    private static KeyInfo[] NormalizePublicKeys(string[] keys)
    {
        return keys
            .Select(k => k.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length == 2)
            .Select(parts => new KeyInfo(parts[0].Trim(), parts[1].Trim()))
            .GroupBy(k => k.Name)
            .Select(g => g.Last())
            .ToArray();
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

    private class KeyInfo(string name, string value)
    {
        public string Name { get; } = name;
        public string Value { get; } = value;
    }
}
