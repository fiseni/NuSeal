using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;

namespace NuSeal;

public class License
{
    private static readonly ConcurrentDictionary<string, RsaSecurityKey> _publicKeysCache = new();

    public static string Create(LicenseParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.PrivateKeyPem);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.ProductName);

        var rsaSecurityKey = _publicKeysCache.GetOrAdd(parameters.PrivateKeyPem, static key =>
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(key.AsSpan());
            return new RsaSecurityKey(rsa);
        });

        var credentials = new SigningCredentials(
            rsaSecurityKey,
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

        if (!string.IsNullOrWhiteSpace(parameters.ClientId))
        {
            claims.Add(new Claim("client", parameters.ClientId));
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
