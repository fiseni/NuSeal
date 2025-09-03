using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NuSeal;

public sealed class LicenseValidator
{
    public static bool IsValid(string publicKeyPem, string license, string productName)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem)
            || string.IsNullOrWhiteSpace(license)
            || string.IsNullOrWhiteSpace(productName))
        {
            return false;
        }

        try
        {
            // Note: ImportFromPem is available in .NET 5.0 and later
            // We'll use BouncyCastle for netstandard2.0
            using var rsa = CreateRsaFromPem(publicKeyPem);
            var key = new RsaSecurityKey(rsa);

            var validationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JsonWebTokenHandler();
            var result = handler.ValidateTokenAsync(license, validationParameters).Result;

            if (result.IsValid is false)
                return false;

            // Parse the token and check the "product" claim
            var jwt = handler.ReadJsonWebToken(license);
            var productClaim = jwt.Claims.FirstOrDefault(c => c.Type == "product")?.Value;

            if (productClaim is null)
                return false;

            return productClaim.Equals(productName, StringComparison.OrdinalIgnoreCase);
        }
        catch { }

        return false;
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
        else
        {
            throw new ArgumentException("PEM string does not contain a valid RSA public key.", nameof(pem));
        }
    }
}
