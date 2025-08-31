namespace NuSeal;

public sealed class LicenseValidator
{
    public static bool IsValid(string license, string publicKey)
    {
        // Dummy logic for time being
        return license.Contains("valid");
    }
}
