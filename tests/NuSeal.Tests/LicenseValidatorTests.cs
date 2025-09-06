using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NuSeal;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Tests;

public class LicenseValidatorTests
{
    private const string _productName = "TestProduct";
    private readonly RsaKeyPair _keyPair;

    public LicenseValidatorTests()
    {
        _keyPair = GenerateRsaKeyPair();
    }

    [Fact]
    public void ReturnsFalse_GivenNullPublicKey()
    {
        var pemData = new PemData(_productName, null!);
        var license = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, license);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyPublicKey()
    {
        var pemData = new PemData(_productName, "");
        var license = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, license);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenWhitespacePublicKey()
    {
        var pemData = new PemData(_productName, "   ");
        var license = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, license);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenNullProductName()
    {
        var pemData = new PemData(null!, _keyPair.PublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, license);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyProductName()
    {
        var pemData = new PemData("", _keyPair.PublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, license);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenWhitespaceProductName()
    {
        var pemData = new PemData("   ", _keyPair.PublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, license);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenNullLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);

        var result = LicenseValidator.IsValid(pemData, null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);

        var result = LicenseValidator.IsValid(pemData, "");

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenWhitespaceLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);

        var result = LicenseValidator.IsValid(pemData, "   ");

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenInvalidLicenseFormat()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var invalidLicense = "not-a-valid-jwt-token";

        var result = LicenseValidator.IsValid(pemData, invalidLicense);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenExpiredLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var expiredLicense = GenerateLicense(
            expirationDate: DateTimeOffset.UtcNow.AddDays(-1),
            startDate: DateTimeOffset.UtcNow.AddDays(-10));

        var result = LicenseValidator.IsValid(pemData, expiredLicense);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenFutureLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var futureLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddDays(10),
            expirationDate: DateTimeOffset.UtcNow.AddDays(20));

        var result = LicenseValidator.IsValid(pemData, futureLicense);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenLicenseWithDifferentProductName()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var licenseWithDifferentProduct = GenerateLicense(productName: "DifferentProduct");

        var result = LicenseValidator.IsValid(pemData, licenseWithDifferentProduct);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenLicenseWithMissingProductClaim()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var licenseWithoutProductClaim = GenerateLicense(includeProductName: false);

        var result = LicenseValidator.IsValid(pemData, licenseWithoutProductClaim);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsFalse_GivenLicenseSignedWithDifferentKey()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var differentKeyPair = GenerateRsaKeyPair();
        var licenseSignedWithDifferentKey = GenerateLicense(differentKeyPair.PrivateKeyPem);

        var result = LicenseValidator.IsValid(pemData, licenseSignedWithDifferentKey);

        result.Should().BeFalse();
    }

    [Fact]
    public void ReturnsTrue_GivenValidLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var validLicense = GenerateValidLicense();

        var result = LicenseValidator.IsValid(pemData, validLicense);

        result.Should().BeTrue();
    }

    [Fact]
    public void ReturnsTrue_GivenValidLicenseWithDifferentCase()
    {
        var pemData = new PemData(_productName.ToUpper(), _keyPair.PublicKeyPem);
        var validLicense = GenerateLicense(productName: _productName.ToLower());

        var result = LicenseValidator.IsValid(pemData, validLicense);

        result.Should().BeTrue();
    }

    private string GenerateValidLicense() => GenerateLicense();

    private string GenerateLicense(
        string? privateKeyPem = null,
        string? productName = null,
        bool includeProductName = true,
        DateTimeOffset? startDate = null,
        DateTimeOffset? expirationDate = null)
    {
        using var rsa = RSA.Create();

        privateKeyPem ??= _keyPair.PrivateKeyPem;
        rsa.ImportFromPem(privateKeyPem.AsSpan());

        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
        };

        if (includeProductName is true)
        {
            claims.Add(new("product", productName ?? _productName));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            NotBefore = startDate?.UtcDateTime ?? DateTimeOffset.UtcNow.AddMinutes(-5).UtcDateTime,
            Expires = expirationDate?.UtcDateTime ?? DateTimeOffset.UtcNow.AddYears(1).UtcDateTime,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return token;
    }

    private static RsaKeyPair GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        return new RsaKeyPair(privateKey, publicKey);
    }

    private record RsaKeyPair(string PrivateKeyPem, string PublicKeyPem);
}
