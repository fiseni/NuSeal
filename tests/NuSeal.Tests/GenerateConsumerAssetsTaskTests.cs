using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Tests;

public class GenerateConsumerAssetsTaskTests : IDisposable
{
    private readonly TestBuildEngine _buildEngine;
    private readonly string _testDirectory;

    public GenerateConsumerAssetsTaskTests()
    {
        _buildEngine = new TestBuildEngine();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenExceptionInProcessing()
    {
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent");

        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = nonExistentPath,
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = GeneratePemTaskItems("ProductA"),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenEmptyNuSealVersion()
    {
        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "",
            ConsumerOutputPath = _testDirectory,
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = GeneratePemTaskItems("ProductA"),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenEmptyConsumerOutputPath()
    {
        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = "",
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = GeneratePemTaskItems("ProductA"),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenEmptyConsumerPackageId()
    {
        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = _testDirectory,
            ConsumerPackageId = "",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = GeneratePemTaskItems("ProductA"),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenEmptyConsumerAssemblyName()
    {
        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = _testDirectory,
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "",
            ConsumerPems = GeneratePemTaskItems("ProductA"),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenEmptyPems()
    {
        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = _testDirectory,
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = Array.Empty<ITaskItem>(),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenPemWithMissingProductName()
    {
        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = _testDirectory,
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = GeneratePemTaskItems("Product1", ""),
            ConsumerPropsFile = null,
            ConsumerTargetsFile = null,
            ValidationMode = null,
            ValidationScope = null,
        };
        var result = task.Execute();

        result.Should().BeFalse();
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnTrueAndGeneratesAssets()
    {
        var consumerPropsFile = Path.Combine(_testDirectory, "consumer.props");
        var consumerProps = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <PropertyGroup>
                    <TestProperty>PropsTestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(consumerPropsFile, consumerProps);

        var consumerTargetsFile = Path.Combine(_testDirectory, "consumer.targets");
        var consumerTargets = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <PropertyGroup>
                    <TestProperty>TargetsTestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(consumerTargetsFile, consumerTargets);

        var expectedProps = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <PropertyGroup>
                <NuSealAssembly_PrefixPackageId1>$([MSBuild]::NormalizePath('$(NugetPackageRoot)', 'nuseal', '1.2.3', 'tasks', 'netstandard2.0', 'NuSeal.dll'))</NuSealAssembly_PrefixPackageId1>
              </PropertyGroup>

              <ItemGroup>
            <Pem_PrefixPackageId1 Include="
            -----BEGIN PUBLIC KEY-----
            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAygsSRxp1MInUqDz2nPk+
            +BPP8ojPdydEg8inQbx7SonV+HBuUfRnbhp/0w298bQP0X1fz+RwnjUDdakV9vsa
            zrK3RH/Ulq0tLrQXKBRZVP2rot4SWWYcdncnvYIiXSpAK2kisxYX1BL56wAEigKX
            CoCmQl8YleATGf2EEZ80tOmL6eEtJZ3rFxcaIbdx6z10XwIkvMM4CgbEPIpGZqva
            lceYsQ/KioeoxbyjBiNOu3DnkjpzhgbDg/dMKMVvZ1DiJBWvaKkToVDpfGFFpwUs
            OEvTfMysHGQ/YqQU+AoGjQJr3/n4X9+THSsXF+Ga7mxMc9x9SwOMebM9q6LDUoG7
            cQIDAQAB
            -----END PUBLIC KEY-----
            " ProductName="ProductA" />
            <Pem_PrefixPackageId1 Include="
            -----BEGIN PUBLIC KEY-----
            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAygsSRxp1MInUqDz2nPk+
            +BPP8ojPdydEg8inQbx7SonV+HBuUfRnbhp/0w298bQP0X1fz+RwnjUDdakV9vsa
            zrK3RH/Ulq0tLrQXKBRZVP2rot4SWWYcdncnvYIiXSpAK2kisxYX1BL56wAEigKX
            CoCmQl8YleATGf2EEZ80tOmL6eEtJZ3rFxcaIbdx6z10XwIkvMM4CgbEPIpGZqva
            lceYsQ/KioeoxbyjBiNOu3DnkjpzhgbDg/dMKMVvZ1DiJBWvaKkToVDpfGFFpwUs
            OEvTfMysHGQ/YqQU+AoGjQJr3/n4X9+THSsXF+Ga7mxMc9x9SwOMebM9q6LDUoG7
            cQIDAQAB
            -----END PUBLIC KEY-----
            " ProductName="ProductB" />
              </ItemGroup>

              <UsingTask
                TaskName="NuSeal.ValidateLicenseTask_0_4_0"
                AssemblyFile="$(NuSealAssembly_PrefixPackageId1)"
                TaskFactory="TaskHostFactory" />


                <PropertyGroup>
                    <TestProperty>PropsTestValue</TestProperty>
                </PropertyGroup>

            </Project>
            """;

        var expectedTargets = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      AfterTargets="AfterBuild"
                      Condition="'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'">

                <NuSeal.ValidateLicenseTask_0_4_0
                  MainAssemblyPath="$(TargetPath)"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
                  Pems="@(Pem_PrefixPackageId1)"
                  ValidationMode="Error"
                  ValidationScope="Transitive"
                  />

              </Target>


                <PropertyGroup>
                    <TestProperty>TargetsTestValue</TestProperty>
                </PropertyGroup>

            </Project>
            """;

        var task = new GenerateConsumerAssetsTask()
        {
            BuildEngine = _buildEngine,
            NuSealAssetsPath = "path/to/nuseal/assets",
            NuSealVersion = "1.2.3",
            ConsumerOutputPath = _testDirectory,
            ConsumerPackageId = "Prefix.PackageId1",
            ConsumerAssemblyName = "Assembly1",
            ConsumerPems = GeneratePemTaskItems("ProductA", "ProductB"),
            ConsumerPropsFile = consumerPropsFile,
            ConsumerTargetsFile = consumerTargetsFile,
            ValidationMode = "Error",
            ValidationScope = "Transitive",
        };
        var result = task.Execute();

        var generatedProps = File.ReadAllText(Path.Combine(_testDirectory, "Prefix.PackageId1.props"));
        var generatedTargets = File.ReadAllText(Path.Combine(_testDirectory, "Prefix.PackageId1.targets"));

        result.Should().BeTrue();
        generatedProps.Should().Be(expectedProps);
        generatedTargets.Should().Be(expectedTargets);
        _buildEngine.Messages.Should().BeEmpty();
        _buildEngine.Warnings.Should().BeEmpty();
        _buildEngine.Errors.Should().BeEmpty();
    }

    private ITaskItem[] GeneratePemTaskItems(params string[] productNames)
    {
        return productNames.Select(productName =>
        {
            var publicKeyPem = """
            -----BEGIN PUBLIC KEY-----
            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAygsSRxp1MInUqDz2nPk+
            +BPP8ojPdydEg8inQbx7SonV+HBuUfRnbhp/0w298bQP0X1fz+RwnjUDdakV9vsa
            zrK3RH/Ulq0tLrQXKBRZVP2rot4SWWYcdncnvYIiXSpAK2kisxYX1BL56wAEigKX
            CoCmQl8YleATGf2EEZ80tOmL6eEtJZ3rFxcaIbdx6z10XwIkvMM4CgbEPIpGZqva
            lceYsQ/KioeoxbyjBiNOu3DnkjpzhgbDg/dMKMVvZ1DiJBWvaKkToVDpfGFFpwUs
            OEvTfMysHGQ/YqQU+AoGjQJr3/n4X9+THSsXF+Ga7mxMc9x9SwOMebM9q6LDUoG7
            cQIDAQAB
            -----END PUBLIC KEY-----
            """;

            var path = Path.Combine(_testDirectory, $"{Guid.NewGuid().ToString()}.pem");
            File.WriteAllText(path, publicKeyPem);
            var taskItem = new TaskItem(path);
            if (!string.IsNullOrWhiteSpace(productName))
            {
                taskItem.SetMetadata("ProductName", productName);
            }
            return taskItem;
        }).ToArray();
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
