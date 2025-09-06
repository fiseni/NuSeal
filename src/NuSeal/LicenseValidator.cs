using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NuSeal;

// I was using Microsoft.IdentityModel.JsonWebTokens to parse tokens.
// But, that package has ungodly amount of dependencies.
// We have to pack all dlls in the tasks folder, so having that dependency is not acceptable.
// We'll parse and validate the token manually.
internal class LicenseValidator
{
    internal static bool IsValid(PemData pem, string license)
    {
        if (string.IsNullOrWhiteSpace(pem.PublicKeyPem)
            || string.IsNullOrWhiteSpace(pem.ProductName)
            || string.IsNullOrWhiteSpace(license))
        {
            return false;
        }

        try
        {
            var parts = license.Split('.');
            if (parts.Length != 3)
                return false;

            if (VerifyHeader(parts) is false)
                return false;

            if (VerifySignature(pem, parts) is false)
                return false;

            var payloadBytes = Base64UrlDecode(parts[1]);
            var payload = JsonDocument.Parse(payloadBytes).RootElement;

            if (VerifyProductName(payload, pem.ProductName) is false)
                return false;

            if (VerifyExpiration(payload) is false)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyHeader(string[] parts)
    {
        var headerBytes = Base64UrlDecode(parts[0]);
        var header = JsonDocument.Parse(headerBytes).RootElement;

        if (!header.TryGetProperty("alg", out var alg))
            return false;

        return string.Equals(alg.GetString(), "RS256", StringComparison.OrdinalIgnoreCase);
    }

    private static bool VerifySignature(PemData pem, string[] parts)
    {
        var signatureBytes = Base64UrlDecode(parts[2]);
        var data = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        using var rsa = CreateRsaFromPem(pem.PublicKeyPem);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return rsa.VerifyHash(hash, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static bool VerifyProductName(JsonElement payload, string productName)
    {
        if (!payload.TryGetProperty("product", out var productClaim))
            return false;

        return string.Equals(productClaim.GetString(), productName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VerifyExpiration(JsonElement payload)
    {
        var clockSkewInMinutes = 5;

        if (payload.TryGetProperty("nbf", out var nbf)
            && nbf.GetInt64() > DateTimeOffset.UtcNow.AddMinutes(-1 * clockSkewInMinutes).ToUnixTimeSeconds())
        {
            return false;
        }

        if (payload.TryGetProperty("exp", out var exp)
            && exp.GetInt64() < DateTimeOffset.UtcNow.AddMinutes(clockSkewInMinutes).ToUnixTimeSeconds())
        {
            return false;
        }

        return true;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static RSA CreateRsaFromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        var obj = pemReader.ReadObject();

        if (obj is RsaKeyParameters rsaKeyParams)
        {
            return DotNetUtilities.ToRSA(rsaKeyParams);
        }

        throw new ArgumentException("PEM string does not contain a valid RSA public key.", nameof(pem));
    }
}
