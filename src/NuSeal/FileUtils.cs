using System.IO;

namespace NuSeal;

internal class FileUtils
{
    internal static bool TryGetLicense(string mainAssemblyPath, string productName, out string licenseContent)
    {
        if (string.IsNullOrWhiteSpace(mainAssemblyPath) || string.IsNullOrWhiteSpace(productName))
        {
            licenseContent = string.Empty;
            return false;
        }

        var licenseFileName = $"{productName}.lic";

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
