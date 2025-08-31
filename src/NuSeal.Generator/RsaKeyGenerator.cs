using System.Security.Cryptography;

namespace NuSeal;

public class RsaKeyGenerator
{
    public static RsaPemPair GeneratePem()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        return new RsaPemPair(publicKey, privateKey);
    }
}

public class RsaPemPair(string publicKey, string privateKey)
{
    public string PublicKey { get; } = publicKey;
    public string PrivateKey { get; } = privateKey;
}
