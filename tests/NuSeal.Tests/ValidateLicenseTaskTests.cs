using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Tests;

public class ValidateLicenseTaskTests : IDisposable
{
    private readonly RsaPemPair _rsaPemPair;
    private readonly TestBuildEngine _buildEngine;
    private readonly string _tempDir;
    private readonly string _mainAssemblyPath;

    public ValidateLicenseTaskTests()
    {
        _rsaPemPair = RsaKeyGenerator.GeneratePem();
        _buildEngine = new TestBuildEngine();
        _tempDir = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _mainAssemblyPath = Path.Combine(_tempDir, "MainApp.dll");
        File.WriteAllText(_mainAssemblyPath, "Dummy content");
    }

    [Fact]
    public void ReturnsTrue_GivenValidLicenseAndDirectScope()
    {
        var productName = "TestProduct";
        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Error",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_GivenValidLicenseAndTransitiveScope()
    {
        var productName = "TestProduct";
        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Error",
            ValidationScope = "Transitive",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsWarning_GivenNoLicenseAndWarningMode()
    {
        var productName = "TestProduct";

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Warning",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().NotBeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenNoLicenseAndErrorMode()
    {
        var productName = "TestProduct";

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Error",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNoPems()
    {
        var productName = "TestProduct";
        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = Array.Empty<ITaskItem>(),
            ValidationMode = "Error",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsWarning_GivenWarningMode_ExpiredWithinGracePeriodLicense()
    {
        var productName = "TestProduct";

        var licenseParameters = new LicenseParameters
        {
            ProductName = productName,
            PrivateKeyPem = _rsaPemPair.PrivateKey,
            ExpirationDate = DateTime.UtcNow.AddDays(-1),
            GracePeriodInDays = 7
        };

        CreateLicense(licenseParameters);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Warning",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().NotBeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsWarning_GivenErrorMode_ExpiredWithinGracePeriodLicense()
    {
        var productName = "TestProduct";

        var licenseParameters = new LicenseParameters
        {
            ProductName = productName,
            PrivateKeyPem = _rsaPemPair.PrivateKey,
            ExpirationDate = DateTime.UtcNow.AddDays(-1),
            GracePeriodInDays = 7
        };

        CreateLicense(licenseParameters);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Error",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().NotBeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsWarning_GivenWarningMode_ExpiredOutsideGracePeriodLicense()
    {
        var productName = "TestProduct";

        var licenseParameters = new LicenseParameters
        {
            ProductName = productName,
            PrivateKeyPem = _rsaPemPair.PrivateKey,
            ExpirationDate = DateTime.UtcNow.AddDays(-10),
            GracePeriodInDays = 1
        };

        CreateLicense(licenseParameters);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Warning",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().NotBeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenErrorMode_ExpiredOutsideGracePeriodLicense()
    {
        var productName = "TestProduct";

        var licenseParameters = new LicenseParameters
        {
            ProductName = productName,
            PrivateKeyPem = _rsaPemPair.PrivateKey,
            ExpirationDate = DateTime.UtcNow.AddDays(-10),
            GracePeriodInDays = 1
        };

        CreateLicense(licenseParameters);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "TestPackageId",
            ProtectedAssemblyName = "TestAssembly",
            Pems = GeneratePemTaskItems(productName),
            ValidationMode = "Error",
            ValidationScope = "Direct",
        };

        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNullMainAssemblyPath()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = null!,
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenEmptyMainAssemblyPath()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenWhitespaceMainAssemblyPath()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "  ",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNullProtectedPackageId()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = null!,
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenEmptyProtectedPackageId()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenWhitespaceProtectedPackageId()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "  ",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNullProtectedAssemblyName()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = null!,
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenEmptyProtectedAssemblyName()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenWhitespaceProtectedAssemblyName()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "  ",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNullPems()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = null!,
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenEmptyPems()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = Array.Empty<ITaskItem>(),
            ValidationMode = "x",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNullValidationMode()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = null!,
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenEmptyValidationMode()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenWhitespaceValidationMode()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "  ",
            ValidationScope = "x",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenNullValidationScope()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = null!,
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenEmptyValidationScope()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenWhitespaceValidationScope()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            MainAssemblyPath = "x",
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
            Pems = GeneratePemTaskItems("x"),
            ValidationMode = "x",
            ValidationScope = "  ",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }


    private void CreateValidLicense(string productName)
    {
        var licenseParams = new LicenseParameters
        {
            ProductName = productName,
            PrivateKeyPem = _rsaPemPair.PrivateKey,
        };
        var license = License.Create(licenseParams);
        var licenseFilePath = Path.Combine(_tempDir, $"{productName}.lic");
        File.WriteAllText(licenseFilePath, license);
    }

    private void CreateLicense(LicenseParameters licenseParameters)
    {
        var license = License.Create(licenseParameters);
        var licenseFilePath = Path.Combine(_tempDir, $"{licenseParameters.ProductName}.lic");
        File.WriteAllText(licenseFilePath, license);
    }

    private ITaskItem[] GeneratePemTaskItems(params string[] productNames)
    {
        return productNames.Select(productName =>
        {
            var taskItem = new TaskItem(_rsaPemPair.PublicKey);
            taskItem.SetMetadata("ProductName", productName);
            return taskItem;
        }).ToArray();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
    }
}
