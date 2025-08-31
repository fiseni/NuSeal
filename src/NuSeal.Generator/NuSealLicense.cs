using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using System.Security.Cryptography;

namespace NuSeal;

public class NuSealLicense
{
    public static string Create(
        string privateKeyPem,
        string subscriptionId,
        string productName,
        string edition,
        string issuer,
        string audience,
        DateTimeOffset startDate,
        DateTimeOffset expirationDate)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.AsSpan());

        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subscriptionId),
            new Claim("product", productName),
            new Claim("edition", edition),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expirationDate.UtcDateTime,
            NotBefore = startDate.UtcDateTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return token;
    }
}
