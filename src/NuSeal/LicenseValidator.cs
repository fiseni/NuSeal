using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NuSeal;

public sealed class LicenseValidator
{
    public static bool IsValid(string publicKeyPem, string license, string productName)
    {
        using var rsa = RSA.Create();

        // Note: ImportFromPem is available in .NET 5.0 and later
        //rsa.ImportFromPem(publicKeyPem.AsSpan());

        var key = new RsaSecurityKey(rsa);

        var validationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = false,
            //ValidateIssuer = true,
            //ValidateAudience = true,
            //ValidIssuer = "a",
            //ValidAudience = "b",
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
}
