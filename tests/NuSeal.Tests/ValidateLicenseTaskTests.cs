using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

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
    public void ReturnsTrue_LogsInfo_GivenExceptionDuringDllProcessing()
    {
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";

        var invalidDllPath = Path.Combine(_tempDir, "Invalid.dll");
        File.WriteAllText(invalidDllPath, "Not a valid DLL");
        var taskItem1 = new TaskItem(invalidDllPath);
        taskItem1.SetMetadata("NuGetPackageId", packageId);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_LogsWarning_GivenNoLicenseAndWarningMode()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: false,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_GivenDirectScope_HasReferences_HasResolvedFiles()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: false,
            productName: "x",
            packageId: "x",
            assemblyName: "x");

        var taskItem2 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem("x"), new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1, taskItem2],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_GivenTransitiveScope_HasReferences_HasResolvedFiles()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_GivenTransitiveScope_NoReferences_HasResolvedFiles()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = Array.Empty<ITaskItem>(),
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_LogsInfo_GivenTransitiveScope_HasReferences_HasResolvedFilesWithInvalidId()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: "x",
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
            ValidationMode = "Error",
            ValidationScope = "Transitive",
        };

        var result = task.Execute();

        result.Should().BeTrue();
        _buildEngine.Messages.Should().NotBeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenDirectScope_HasReferenceWithInvalidId_HasResolvedFiles()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = [new TaskItem("x")],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_LogsInfo_GivenDirectScope_NoReferences_HasResolvedFiles()
    {
        var productName = "TestProduct";
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

        CreateValidLicense(productName);

        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = Array.Empty<ITaskItem>(),
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
    public void ReturnsTrue_LogsInfo_GivenNoReferencesAndNoResolvedFiles()
    {
        var task = new ValidateLicenseTask
        {
            BuildEngine = _buildEngine,
            PackageReferences = Array.Empty<ITaskItem>(),
            ResolvedCompileFileDefinitions = Array.Empty<ITaskItem>(),
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = "x",
            ProtectedAssemblyName = "x",
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
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

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
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

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
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

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
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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
        var packageId = "TestPackageId";
        var assemblyName = "TestAssembly";
        var taskItem1 = CreateResolvedCompileFile(
            includePems: true,
            productName: productName,
            packageId: packageId,
            assemblyName: assemblyName);

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
            PackageReferences = [new TaskItem(packageId)],
            ResolvedCompileFileDefinitions = [taskItem1],
            MainAssemblyPath = _mainAssemblyPath,
            ProtectedPackageId = packageId,
            ProtectedAssemblyName = assemblyName,
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

    private ITaskItem CreateResolvedCompileFile(
        bool includePems = true,
        string productName = "TestProduct",
        string packageId = "TestPackageId",
        string assemblyName = "TestAssembly")
    {
        var dllPath = CreateDll(includePems, productName, assemblyName);
        var taskItem = new TaskItem(dllPath);
        taskItem.SetMetadata("NuGetPackageId", packageId);
        return taskItem;
    }

    private string CreateDll(
        bool includePems = true,
        string productName = "TestProduct",
        string assemblyName = "TestAssembly")
    {
        var dllPath = Path.Combine(_tempDir, $"{assemblyName}.dll");

        // Create a test assembly
        var assemblyDef = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0)),
            "TestModule",
            ModuleKind.Dll);

        // Add PEM resource if needed
        if (includePems)
        {
            var pemContent = _rsaPemPair.PublicKey;
            var resourceName = $"TestCompany.{productName}.nuseal.pem";
            var resource = new EmbeddedResource(
                resourceName,
                Mono.Cecil.ManifestResourceAttributes.Public,
                System.Text.Encoding.UTF8.GetBytes(pemContent));
            assemblyDef.MainModule.Resources.Add(resource);
        }

        // Save the assembly
        assemblyDef.Write(dllPath);

        return dllPath;
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
