using System.IO;

namespace NuSeal;

internal class FileUtils
{
    internal static bool TryGetLicense(string targetAssemblyPath, string productName, out string licenseContent)
    {
        if (string.IsNullOrWhiteSpace(targetAssemblyPath) || string.IsNullOrWhiteSpace(productName))
        {
            licenseContent = string.Empty;
            return false;
        }

        var licenseFileName = $"{productName}.lic";

        try
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(targetAssemblyPath)!);
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
