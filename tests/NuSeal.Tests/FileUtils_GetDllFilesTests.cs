namespace Tests;

public class FileUtils_GetDllFilesTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _mainAssemblyPath;

    public FileUtils_GetDllFilesTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _mainAssemblyPath = Path.Combine(_testDirectory, "MainAssembly.dll");

        // Create the main assembly file
        File.WriteAllText(_mainAssemblyPath, "dummy content");
    }

    [Fact]
    public void ReturnsEmptyArray_GivenEmptyDirectory()
    {
        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExcludesMainAssembly_GivenDirectoryWithMainAssembly()
    {
        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().BeEmpty();
        result.Should().NotContain(_mainAssemblyPath);
    }

    [Fact]
    public void IncludesCustomDlls_GivenDirectoryWithCustomDlls()
    {
        // Arrange
        string customDll1 = Path.Combine(_testDirectory, "Custom1.dll");
        string customDll2 = Path.Combine(_testDirectory, "Custom2.dll");

        File.WriteAllText(customDll1, "dummy content");
        File.WriteAllText(customDll2, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(customDll1);
        result.Should().Contain(customDll2);
    }

    [Fact]
    public void ExcludesNuSealDlls_GivenDirectoryWithNuSealDlls()
    {
        // Arrange
        string customDll = Path.Combine(_testDirectory, "Custom.dll");
        string nuSealDll = Path.Combine(_testDirectory, "NuSeal.Core.dll");

        File.WriteAllText(customDll, "dummy content");
        File.WriteAllText(nuSealDll, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(customDll);
        result.Should().NotContain(nuSealDll);
    }

    [Fact]
    public void ExcludesSystemDlls_GivenDirectoryWithSystemDlls()
    {
        // Arrange
        string customDll = Path.Combine(_testDirectory, "Custom.dll");
        string systemDll = Path.Combine(_testDirectory, "System.Text.Json.dll");

        File.WriteAllText(customDll, "dummy content");
        File.WriteAllText(systemDll, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(customDll);
        result.Should().NotContain(systemDll);
    }

    [Fact]
    public void ExcludesMicrosoftDlls_GivenDirectoryWithMicrosoftDlls()
    {
        // Arrange
        string customDll = Path.Combine(_testDirectory, "Custom.dll");
        string microsoftDll = Path.Combine(_testDirectory, "Microsoft.Extensions.Logging.dll");

        File.WriteAllText(customDll, "dummy content");
        File.WriteAllText(microsoftDll, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(customDll);
        result.Should().NotContain(microsoftDll);
    }

    [Fact]
    public void ExcludesNetStandardDlls_GivenDirectoryWithNetStandardDlls()
    {
        // Arrange
        string customDll = Path.Combine(_testDirectory, "Custom.dll");
        string netStandardDll = Path.Combine(_testDirectory, "netstandard.dll");

        File.WriteAllText(customDll, "dummy content");
        File.WriteAllText(netStandardDll, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(customDll);
        result.Should().NotContain(netStandardDll);
    }

    [Fact]
    public void ExcludesWindowsDlls_GivenDirectoryWithWindowsDlls()
    {
        // Arrange
        string customDll = Path.Combine(_testDirectory, "Custom.dll");
        string windowsDll = Path.Combine(_testDirectory, "Windows.Storage.dll");

        File.WriteAllText(customDll, "dummy content");
        File.WriteAllText(windowsDll, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(customDll);
        result.Should().NotContain(windowsDll);
    }

    [Fact]
    public void FiltersCorrectly_GivenMixedDirectoryContent()
    {
        // Arrange
        string customDll1 = Path.Combine(_testDirectory, "Custom1.dll");
        string customDll2 = Path.Combine(_testDirectory, "Custom2.dll");
        string nuSealDll = Path.Combine(_testDirectory, "NuSeal.Core.dll");
        string systemDll = Path.Combine(_testDirectory, "System.Text.Json.dll");
        string microsoftDll = Path.Combine(_testDirectory, "Microsoft.Extensions.Logging.dll");
        string netStandardDll = Path.Combine(_testDirectory, "netstandard.dll");
        string windowsDll = Path.Combine(_testDirectory, "Windows.Storage.dll");
        string nonDllFile = Path.Combine(_testDirectory, "SomeFile.txt");

        File.WriteAllText(customDll1, "dummy content");
        File.WriteAllText(customDll2, "dummy content");
        File.WriteAllText(nuSealDll, "dummy content");
        File.WriteAllText(systemDll, "dummy content");
        File.WriteAllText(microsoftDll, "dummy content");
        File.WriteAllText(netStandardDll, "dummy content");
        File.WriteAllText(windowsDll, "dummy content");
        File.WriteAllText(nonDllFile, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(customDll1);
        result.Should().Contain(customDll2);
        result.Should().NotContain(nuSealDll);
        result.Should().NotContain(systemDll);
        result.Should().NotContain(microsoftDll);
        result.Should().NotContain(netStandardDll);
        result.Should().NotContain(windowsDll);
        result.Should().NotContain(_mainAssemblyPath);
        // Non-dll files are not included by default due to the *.dll filter
    }

    [Fact]
    public void PerformsCaseInsensitiveComparison_GivenDifferentCaseDlls()
    {
        // Arrange
        string customDll = Path.Combine(_testDirectory, "Custom.dll");
        string systemDllLowerCase = Path.Combine(_testDirectory, "system.text.json.dll");
        string nuSealDllMixedCase = Path.Combine(_testDirectory, "NuSeal.CORE.dll");

        File.WriteAllText(customDll, "dummy content");
        File.WriteAllText(systemDllLowerCase, "dummy content");
        File.WriteAllText(nuSealDllMixedCase, "dummy content");

        // Act
        string[] result = FileUtils.GetDllFiles(_testDirectory, _mainAssemblyPath);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(customDll);
        result.Should().NotContain(systemDllLowerCase);
        result.Should().NotContain(nuSealDllMixedCase);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDirectory, true);
        }
        catch
        {
        }
    }
}
