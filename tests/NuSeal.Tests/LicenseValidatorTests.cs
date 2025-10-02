using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
    public void ReturnsValid_GivenValidLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var validLicense = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, validLicense);

        result.Should().Be(LicenseValidationResult.Valid);
    }

    [Fact]
    public void ReturnsValid_GivenValidLicenseWithDifferentProductCase()
    {
        var pemData = new PemData(_productName.ToUpper(), _keyPair.PublicKeyPem);
        var validLicense = GenerateLicense(productName: _productName.ToLower());

        var result = LicenseValidator.Validate(pemData, validLicense);

        result.Should().Be(LicenseValidationResult.Valid);
    }

    [Fact]
    public void ReturnsValid_GivenExpiredLicenseWithinClockSkew()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var expiredLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddDays(-10),
            expirationDate: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = LicenseValidator.Validate(pemData, expiredLicense);

        result.Should().Be(LicenseValidationResult.Valid);
    }

    [Fact]
    public void ReturnsValid_GivenFutureLicenseWithinClockSkew()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var futureLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddMinutes(1),
            expirationDate: DateTimeOffset.UtcNow.AddDays(20));

        var result = LicenseValidator.Validate(pemData, futureLicense);

        result.Should().Be(LicenseValidationResult.Valid);
    }

    [Fact]
    public void ReturnsInvalid_GivenFutureLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var futureLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddDays(10),
            expirationDate: DateTimeOffset.UtcNow.AddDays(20));

        var result = LicenseValidator.Validate(pemData, futureLicense);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsExpiredOutsideGracePeriod_GivenExpiredLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var expiredLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddDays(-10),
            expirationDate: DateTimeOffset.UtcNow.AddDays(-1));

        var result = LicenseValidator.Validate(pemData, expiredLicense);

        result.Should().Be(LicenseValidationResult.ExpiredOutsideGracePeriod);
    }

    [Fact]
    public void ReturnsExpiredOutsideGracePeriod_GivenExpiredLicenseOutsideGracePeriod()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var expiredLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddDays(-20),
            expirationDate: DateTimeOffset.UtcNow.AddDays(-10),
            gracePeriodDays: 7);

        var result = LicenseValidator.Validate(pemData, expiredLicense);

        result.Should().Be(LicenseValidationResult.ExpiredOutsideGracePeriod);
    }

    [Fact]
    public void ReturnsExpiredWithinGracePeriod_GivenExpiredLicenseWithinGracePeriod()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var expiredLicense = GenerateLicense(
            startDate: DateTimeOffset.UtcNow.AddDays(-10),
            expirationDate: DateTimeOffset.UtcNow.AddDays(-2),
            gracePeriodDays: 7);

        var result = LicenseValidator.Validate(pemData, expiredLicense);

        result.Should().Be(LicenseValidationResult.ExpiredWithinGracePeriod);
    }

    [Fact]
    public void ReturnsInvalid_GivenNoStartDate()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var license = GenerateLicense(
            includeStartDate: false);

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenNoEndDate()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var license = GenerateLicense(
            includeEndDate: false);

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenLicenseWithDifferentProductName()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var licenseWithDifferentProduct = GenerateLicense(productName: "DifferentProduct");

        var result = LicenseValidator.Validate(pemData, licenseWithDifferentProduct);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenLicenseWithMissingProductClaim()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var licenseWithoutProductClaim = GenerateLicense(includeProductName: false);

        var result = LicenseValidator.Validate(pemData, licenseWithoutProductClaim);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenInvalidPem()
    {
        var invalidPublicKeyPem = "invalid-public-key-pem";
        var pemData = new PemData(_productName, invalidPublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenMissingAlgHeader()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var license = GenerateJwtWithoutAlg();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenInvalidLicenseFormat()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var invalidLicense = "not-a-valid-jwt-token";

        var result = LicenseValidator.Validate(pemData, invalidLicense);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenLicenseSignedWithDifferentKey()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);
        var differentKeyPair = GenerateRsaKeyPair();
        var licenseSignedWithDifferentKey = GenerateLicense(differentKeyPair.PrivateKeyPem);

        var result = LicenseValidator.Validate(pemData, licenseSignedWithDifferentKey);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenNullPublicKey()
    {
        var pemData = new PemData(_productName, null!);
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenEmptyPublicKey()
    {
        var pemData = new PemData(_productName, "");
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenWhitespacePublicKey()
    {
        var pemData = new PemData(_productName, "   ");
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenNullProductName()
    {
        var pemData = new PemData(null!, _keyPair.PublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenEmptyProductName()
    {
        var pemData = new PemData("", _keyPair.PublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenWhitespaceProductName()
    {
        var pemData = new PemData("   ", _keyPair.PublicKeyPem);
        var license = GenerateValidLicense();

        var result = LicenseValidator.Validate(pemData, license);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenNullLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);

        var result = LicenseValidator.Validate(pemData, null!);

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenEmptyLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);

        var result = LicenseValidator.Validate(pemData, "");

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    [Fact]
    public void ReturnsInvalid_GivenWhitespaceLicense()
    {
        var pemData = new PemData(_productName, _keyPair.PublicKeyPem);

        var result = LicenseValidator.Validate(pemData, "   ");

        result.Should().Be(LicenseValidationResult.Invalid);
    }

    private string GenerateValidLicense() => GenerateLicense();

    private string GenerateLicense(
        string? privateKeyPem = null,
        string? productName = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? expirationDate = null,
        int? gracePeriodDays = null,
        bool includeProductName = true,
        bool includeStartDate = true,
        bool includeEndDate = true)
    {
        using var rsa = RSA.Create();

        privateKeyPem ??= _keyPair.PrivateKeyPem;
        rsa.ImportFromPem(privateKeyPem.AsSpan());

        var handler = new JsonWebTokenHandler();

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

        if (gracePeriodDays.HasValue)
        {
            claims.Add(new("grace_period_days", gracePeriodDays.Value.ToString(), ClaimValueTypes.Integer32));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credentials,
        };

        if (includeStartDate)
        {
            tokenDescriptor.NotBefore = startDate?.UtcDateTime ?? DateTimeOffset.UtcNow.UtcDateTime;
        }
        else
        {
            handler.SetDefaultTimesOnTokenCreation = false;
        }

        if (includeEndDate)
        {
            tokenDescriptor.Expires = expirationDate?.UtcDateTime ?? DateTimeOffset.UtcNow.AddYears(1).UtcDateTime;
        }
        else
        {
            handler.SetDefaultTimesOnTokenCreation = false;
        }

        var token = handler.CreateToken(tokenDescriptor);

        return token;
    }

    public static string GenerateJwtWithoutAlg()
    {
        var header = new Dictionary<string, object> { { "typ", "JWT" } }; // no "alg"
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-sub" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() }
        };

        string headerJson = JsonSerializer.Serialize(header);
        string payloadJson = JsonSerializer.Serialize(payload);

        string headerPart = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        string payloadPart = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signaturePart = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        return $"{headerPart}.{payloadPart}.{signaturePart}";

        static string Base64UrlEncode(byte[] bytes)
        {
            string s = Convert.ToBase64String(bytes);
            s = s.TrimEnd('=');
            s = s.Replace('+', '-').Replace('/', '_');
            return s;
        }
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
