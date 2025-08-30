using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace NuSeal;

public class LicenseValidationTask : Task
{
    private const string _licenseFileName = "NuSeal.license";

    [Required]
    public string MainAssemblyPath { get; set; } = "";

    public string[] LicensePublicKeys { get; set; } = [];

    public override bool Execute()
    {
        bool found = false;
        string licenseFilePath = string.Empty;

        Log.LogMessage(MessageImportance.High, "MainAssemblyPath: " + MainAssemblyPath);

        foreach (var key in LicensePublicKeys)
        {
            Log.LogMessage(MessageImportance.High, "PublicKey: " + key);
        }

        var LicensePublicKey = LicensePublicKeys.FirstOrDefault();

        try
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(MainAssemblyPath)!);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, _licenseFileName);
                if (File.Exists(candidate))
                {
                    found = true;
                    licenseFilePath = candidate;
                    break;
                }
                dir = dir.Parent;
            }
        }
        catch (Exception ex)
        {
            Log.LogWarningFromException(ex, true);
        }

        if (found is false)
        {
            Log.LogError("NuSeal license file (NuSeal.license) not found. Please place it at the solution or repository root.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(LicensePublicKey))
        {
            // Public key is not provided, this shouldn't happen with properly configured products
            Log.LogWarning("No public key provided for license validation. Please contact the package provider.");
            return true;
        }

        try
        {
            var licenseContent = File.ReadAllText(licenseFilePath);
            if (string.IsNullOrWhiteSpace(licenseContent))
            {
                Log.LogError($"NuSeal license file is empty: {licenseFilePath}");
                return false;
            }

            // This is a placeholder for your actual license validation logic
            // You would implement real RSA validation here using the provided public key
            bool isValid = ValidateLicense(licenseContent, LicensePublicKey);

            if (!isValid)
            {
                Log.LogError($"NuSeal license file is invalid: {licenseFilePath}");
                return false;
            }

            Log.LogMessage(MessageImportance.High, $"NuSeal license is valid: {licenseFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to validate license: {ex.Message}");
            return false;
        }
    }

    private bool ValidateLicense(string licenseContent, string publicKey)
    {
        try
        {
            return licenseContent.Contains("valid") || licenseContent.Contains("good");
        }
        catch
        {
            return false;
        }
    }
}
