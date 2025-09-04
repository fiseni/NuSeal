using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuSeal;
internal class Utils
{
    private const string pemFileSuffix = "nuseal.pem";
    private static readonly char[] _resourceNameDelimiter = new[] { '.' };

    internal static List<PemConfig> ExtractPemFromAssembly(AssemblyDefinition assembly)
    {
        var pemConfigs = new List<PemConfig>();

        if (assembly.MainModule.HasResources is false)
        {
            return pemConfigs;
        }

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

    internal static string[] GetDllFiles(string directory, string mainAssemblyPath)
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

    internal static bool TryGetLicenseContent(string mainAssemblyPath, string productName, out string licenseContent)
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
}
