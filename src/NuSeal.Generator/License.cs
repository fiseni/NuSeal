using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;

namespace NuSeal;

public class License
{
    public static string Create(LicenseParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.PrivateKeyPem);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.ProductName);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(parameters.PrivateKeyPem.AsSpan());

        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub, parameters.SubscriptionId),
            new("product", parameters.ProductName),
        };

        if (!string.IsNullOrWhiteSpace(parameters.Edition))
        {
            claims.Add(new Claim("edition", parameters.Edition));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = parameters.StartDate.UtcDateTime,
            Expires = parameters.ExpirationDate.UtcDateTime,
            Issuer = parameters.Issuer,
            Audience = parameters.Audience,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return token;
    }
}
