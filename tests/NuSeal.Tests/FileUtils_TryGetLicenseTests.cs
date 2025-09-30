namespace Tests;

public class FileUtils_TryGetLicenseTests : IDisposable
{
    private readonly string _testRootDirectory;
    private readonly string _subDirectory;
    private readonly string _subSubDirectory;
    private readonly string _mainAssemblyPath;
    private readonly string _productName;

    public FileUtils_TryGetLicenseTests()
    {
        // Create a unique test directory structure for each test run
        _testRootDirectory = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        _subDirectory = Path.Combine(_testRootDirectory, "SubDir");
        _subSubDirectory = Path.Combine(_subDirectory, "SubSubDir");

        Directory.CreateDirectory(_testRootDirectory);
        Directory.CreateDirectory(_subDirectory);
        Directory.CreateDirectory(_subSubDirectory);

        _mainAssemblyPath = Path.Combine(_subSubDirectory, "MainAssembly.dll");
        File.WriteAllText(_mainAssemblyPath, "dummy content");

        _productName = "TestProduct";
    }

    [Fact]
    public void ReturnsFalse_GivenNoLicenseFile()
    {
        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string licenseContent);

        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_GivenLicenseFileInSameDirectory()
    {
        var licenseContent = "license-content";
        var licenseFilePath = Path.Combine(_subSubDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        result.Should().BeTrue();
        retrievedContent.Should().Be(licenseContent);
    }

    [Fact]
    public void ReturnsTrue_GivenLicenseFileInParentDirectory()
    {
        var licenseContent = "license-content";
        var licenseFilePath = Path.Combine(_subDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        result.Should().BeTrue();
        retrievedContent.Should().Be(licenseContent);
    }

    [Fact]
    public void ReturnsTrue_GivenLicenseFileInRootDirectory()
    {
        var licenseContent = "license-content";
        var licenseFilePath = Path.Combine(_testRootDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        result.Should().BeTrue();
        retrievedContent.Should().Be(licenseContent);
    }

    [Fact]
    public void ReturnsFirstLicenseFound_GivenMultipleLicenseFiles()
    {
        var licenseContentRoot = "license-content-root";
        var licenseContentParent = "license-content-parent";
        var licenseContentSame = "license-content-same";

        File.WriteAllText(Path.Combine(_testRootDirectory, $"{_productName}.license"), licenseContentRoot);
        File.WriteAllText(Path.Combine(_subDirectory, $"{_productName}.license"), licenseContentParent);
        File.WriteAllText(Path.Combine(_subSubDirectory, $"{_productName}.license"), licenseContentSame);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        result.Should().BeTrue();
        // Should find the one in the same directory first
        retrievedContent.Should().Be(licenseContentSame);
    }

    [Fact]
    public void TrimsLicenseContent_GivenLicenseWithWhitespace()
    {
        var licenseContent = "  license-content-with-whitespace  \r\n  ";
        var licenseFilePath = Path.Combine(_subSubDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        result.Should().BeTrue();
        retrievedContent.Should().Be("license-content-with-whitespace");
    }

    [Fact]
    public void ReturnsFalse_GivenInvalidMainAssemblyPath()
    {
        var invalidPath = Path.Combine(_subSubDirectory, "NonExistent.dll");

        var result = FileUtils.TryGetLicense(invalidPath, _productName, out string licenseContent);

        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_GivenNullMainAssemblyPath()
    {
        var result = FileUtils.TryGetLicense(null!, _productName, out string licenseContent);

        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyMainAssemblyPath()
    {
        var result = FileUtils.TryGetLicense("", _productName, out string licenseContent);

        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void UsesProductNameForLicenseFile_GivenDifferentProductNames()
    {
        var licenseContent = "license-content";
        var otherProductName = "OtherProduct";

        // Create license file for a different product
        File.WriteAllText(Path.Combine(_subSubDirectory, $"{otherProductName}.license"), licenseContent);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        result.Should().BeFalse();
        retrievedContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyProductName()
    {
        var licenseContent = "license-content";
        // Create license file with empty name
        File.WriteAllText(Path.Combine(_subSubDirectory, ".license"), licenseContent);

        var result = FileUtils.TryGetLicense(_mainAssemblyPath, "", out string retrievedContent);

        result.Should().BeFalse();
        retrievedContent.Should().BeEmpty();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testRootDirectory, true);
        }
        catch
        {
        }
    }
}
