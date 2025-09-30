using Mono.Cecil;

namespace Tests;

public class AssemblyUtils_ExtractPemsTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly AssemblyDefinition _testAssembly;

    public AssemblyUtils_ExtractPemsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var assemblyName = new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0));
        _testAssembly = AssemblyDefinition.CreateAssembly(
            assemblyName,
            "TestModule",
            ModuleKind.Dll);
    }

    [Fact]
    public void ReturnsEmptyList_GivenAssemblyWithNoResources()
    {
        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsEmptyList_GivenAssemblyWithNonPemResources()
    {
        var resource = new EmbeddedResource("TestResource.txt",
            ManifestResourceAttributes.Public,
            new byte[] { 1, 2, 3 });
        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsEmptyList_GivenAssemblyWithPemResourceButInvalidNameFormat()
    {
        // Resource name doesn't have enough parts separated by dots
        var resource = new EmbeddedResource("nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes("test content"));
        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsPemData_GivenAssemblyWithValidPemResource()
    {
        var productName = "TestProduct";
        var pemContent = "-----BEGIN PUBLIC KEY-----\nMIIB...etc\n-----END PUBLIC KEY-----";
        var resource = new EmbeddedResource($"namespace.{productName}.nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pemContent));

        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be(productName);
        result[0].PublicKeyPem.Should().Be(pemContent);
    }

    [Fact]
    public void ReturnsMultiplePemData_GivenAssemblyWithMultiplePemResources()
    {
        var product1 = "Product1";
        var product2 = "Product2";
        var pem1 = "PEM1 Content";
        var pem2 = "PEM2 Content";

        var resource1 = new EmbeddedResource($"namespace.{product1}.nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pem1));

        var resource2 = new EmbeddedResource($"namespace.{product2}.nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pem2));

        _testAssembly.MainModule.Resources.Add(resource1);
        _testAssembly.MainModule.Resources.Add(resource2);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var pemData1 = result.FirstOrDefault(p => p.ProductName == product1);
        var pemData2 = result.FirstOrDefault(p => p.ProductName == product2);

        pemData1.Should().NotBeNull();
        pemData2.Should().NotBeNull();
        pemData1?.PublicKeyPem.Should().Be(pem1);
        pemData2?.PublicKeyPem.Should().Be(pem2);
    }

    [Fact]
    public void ReturnsPemData_GivenResourceWithoutNamespace()
    {
        var productName = "TestProduct";
        var pemContent = "PEM Content";

        // Use uppercase in the suffix
        var resource = new EmbeddedResource($"{productName}.nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pemContent));

        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be(productName);
        result[0].PublicKeyPem.Should().Be(pemContent);
    }

    [Fact]
    public void IsCaseInsensitive_GivenResourceWithDifferentCase()
    {
        var productName = "TestProduct";
        var pemContent = "PEM Content";

        var resource = new EmbeddedResource($"namespace.{productName}.NUSEAL.PEM",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pemContent));

        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be(productName);
        result[0].PublicKeyPem.Should().Be(pemContent);
    }

    [Fact]
    public void HandlesLongerResourceNames_GivenComplexResourceName()
    {
        var productName = "TestProduct";
        var pemContent = "PEM Content";

        // Use a longer resource name with more dots
        var resource = new EmbeddedResource($"some.complex.namespace.{productName}.nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pemContent));

        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be(productName);
        result[0].PublicKeyPem.Should().Be(pemContent);
    }

    [Fact]
    public void ExtractsPemContent_GivenResourceWithMultilineContent()
    {
        var productName = "TestProduct";
        var pemContent = "Line1\nLine2\nLine3";

        var resource = new EmbeddedResource($"namespace.{productName}.nuseal.pem",
            ManifestResourceAttributes.Public,
            GetUtf8Bytes(pemContent));

        _testAssembly.MainModule.Resources.Add(resource);

        var result = AssemblyUtils.ExtractPems(_testAssembly);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PublicKeyPem.Should().Be(pemContent);
    }

    private static byte[] GetUtf8Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
            _testAssembly?.Dispose();
        }
        catch
        {
        }
    }
}
