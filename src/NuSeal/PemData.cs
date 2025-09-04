namespace NuSeal;

internal class PemData
{
    public PemData(string productName, string publicKeyPem)
    {
        ProductName = productName;
        PublicKeyPem = publicKeyPem;
    }

    public string ProductName { get; }
    public string PublicKeyPem { get; }
}
