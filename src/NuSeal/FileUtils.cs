using System;
using System.IO;
using System.Linq;

namespace NuSeal;

internal class FileUtils
{
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

    internal static bool TryGetLicense(string mainAssemblyPath, string productName, out string licenseContent)
    {
        if (string.IsNullOrWhiteSpace(mainAssemblyPath) || string.IsNullOrWhiteSpace(productName))
        {
            licenseContent = string.Empty;
            return false;
        }

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
