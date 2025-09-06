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
        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string licenseContent);

        // Assert
        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_GivenLicenseFileInSameDirectory()
    {
        // Arrange
        string licenseContent = "license-content";
        string licenseFilePath = Path.Combine(_subSubDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        // Assert
        result.Should().BeTrue();
        retrievedContent.Should().Be(licenseContent);
    }

    [Fact]
    public void ReturnsTrue_GivenLicenseFileInParentDirectory()
    {
        // Arrange
        string licenseContent = "license-content";
        string licenseFilePath = Path.Combine(_subDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        // Assert
        result.Should().BeTrue();
        retrievedContent.Should().Be(licenseContent);
    }

    [Fact]
    public void ReturnsTrue_GivenLicenseFileInRootDirectory()
    {
        // Arrange
        string licenseContent = "license-content";
        string licenseFilePath = Path.Combine(_testRootDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        // Assert
        result.Should().BeTrue();
        retrievedContent.Should().Be(licenseContent);
    }

    [Fact]
    public void ReturnsFirstLicenseFound_GivenMultipleLicenseFiles()
    {
        // Arrange
        string licenseContentRoot = "license-content-root";
        string licenseContentParent = "license-content-parent";
        string licenseContentSame = "license-content-same";

        // Create license files in all directories
        File.WriteAllText(Path.Combine(_testRootDirectory, $"{_productName}.license"), licenseContentRoot);
        File.WriteAllText(Path.Combine(_subDirectory, $"{_productName}.license"), licenseContentParent);
        File.WriteAllText(Path.Combine(_subSubDirectory, $"{_productName}.license"), licenseContentSame);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        // Assert
        result.Should().BeTrue();
        // Should find the one in the same directory first
        retrievedContent.Should().Be(licenseContentSame);
    }

    [Fact]
    public void TrimsLicenseContent_GivenLicenseWithWhitespace()
    {
        // Arrange
        string licenseContent = "  license-content-with-whitespace  \r\n  ";
        string licenseFilePath = Path.Combine(_subSubDirectory, $"{_productName}.license");
        File.WriteAllText(licenseFilePath, licenseContent);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        // Assert
        result.Should().BeTrue();
        retrievedContent.Should().Be("license-content-with-whitespace");
    }

    [Fact]
    public void ReturnsFalse_GivenInvalidMainAssemblyPath()
    {
        // Arrange
        string invalidPath = Path.Combine(_subSubDirectory, "NonExistent.dll");

        // Act
        bool result = FileUtils.TryGetLicense(invalidPath, _productName, out string licenseContent);

        // Assert
        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_GivenNullMainAssemblyPath()
    {
        // Act
        bool result = FileUtils.TryGetLicense(null!, _productName, out string licenseContent);

        // Assert
        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyMainAssemblyPath()
    {
        // Act
        bool result = FileUtils.TryGetLicense("", _productName, out string licenseContent);

        // Assert
        result.Should().BeFalse();
        licenseContent.Should().BeEmpty();
    }

    [Fact]
    public void UsesProductNameForLicenseFile_GivenDifferentProductNames()
    {
        // Arrange
        string licenseContent = "license-content";
        string otherProductName = "OtherProduct";

        // Create license file for a different product
        File.WriteAllText(Path.Combine(_subSubDirectory, $"{otherProductName}.license"), licenseContent);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, _productName, out string retrievedContent);

        // Assert
        result.Should().BeFalse();
        retrievedContent.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_GivenEmptyProductName()
    {
        // Arrange
        string licenseContent = "license-content";
        // Create license file with empty name
        File.WriteAllText(Path.Combine(_subSubDirectory, ".license"), licenseContent);

        // Act
        bool result = FileUtils.TryGetLicense(_mainAssemblyPath, "", out string retrievedContent);

        // Assert
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
