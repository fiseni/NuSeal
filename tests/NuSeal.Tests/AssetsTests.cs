namespace Tests;

public class AssetsTests : IDisposable
{
    private readonly string _testDirectory;

    public AssetsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void GenerateProps_ReturnUniqueAsset()
    {
        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            pems: GeneratePemData("ProductA", "ProductB"),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: null,
            validationScope: null);

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <PropertyGroup>
                <NuSealAssembly_1_2_3>$([MSBuild]::NormalizePath('$(NugetPackageRoot)', 'nuseal', '1.2.3', 'tasks', 'netstandard2.0', 'NuSeal_1_2_3.dll'))</NuSealAssembly_1_2_3>
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
                AssemblyFile="$(NuSealAssembly_1_2_3)"
                TaskFactory="TaskHostFactory" />

            </Project>
            """;

        var result = Assets.GenerateProps(parameters);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void GenerateTargets_ReturnUniqueAsset_GivenDirectScope()
    {
        var parameters1 = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            pems: GeneratePemData("ProductA"),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: "Warning",
            validationScope: "Direct");

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      AfterTargets="AfterBuild"
                      >

                <NuSeal.ValidateLicenseTask_0_4_0
                  TargetAssemblyPath="$(TargetPath)"
                  NuSealVersion="1.2.3"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
                  Pems="@(Pem_PrefixPackageId1)"
                  ValidationMode="Warning"
                  ValidationScope="Direct"
                  />

              </Target>

            </Project>
            """;

        var result = Assets.GenerateTargets(parameters1);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void GenerateTargets_ReturnUniqueAsset_GivenTransitiveScope()
    {
        var parameters1 = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            pems: GeneratePemData("ProductA"),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: "Warning",
            validationScope: "Transitive");

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      AfterTargets="AfterBuild"
                      Condition="'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'">

                <NuSeal.ValidateLicenseTask_0_4_0
                  TargetAssemblyPath="$(TargetPath)"
                  NuSealVersion="1.2.3"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
                  Pems="@(Pem_PrefixPackageId1)"
                  ValidationMode="Warning"
                  ValidationScope="Transitive"
                  />

              </Target>

            </Project>
            """;

        var result = Assets.GenerateTargets(parameters1);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void GenerateTargets_ReturnUniqueAsset_GivenDirectScopeAndCondition()
    {
        var parameters1 = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            pems: GeneratePemData("ProductA"),
            condition: "'#(OutputType)' == 'Exe' Or '#(OutputType)' == 'WinExe'",
            propsFile: null,
            targetsFile: null,
            validationMode: "Warning",
            validationScope: "Direct");

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      AfterTargets="AfterBuild"
                      Condition="'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'">

                <NuSeal.ValidateLicenseTask_0_4_0
                  TargetAssemblyPath="$(TargetPath)"
                  NuSealVersion="1.2.3"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
                  Pems="@(Pem_PrefixPackageId1)"
                  ValidationMode="Warning"
                  ValidationScope="Direct"
                  />

              </Target>

            </Project>
            """;

        var result = Assets.GenerateTargets(parameters1);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void GenerateTargets_ReturnUniqueAsset_GivenTransitiveScopeAndCondition()
    {
        var parameters1 = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            pems: GeneratePemData("ProductA"),
            condition: "'#(OutputType)' == 'Exe'",
            propsFile: null,
            targetsFile: null,
            validationMode: "Warning",
            validationScope: "Transitive");

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      AfterTargets="AfterBuild"
                      Condition="'$(OutputType)' == 'Exe'">

                <NuSeal.ValidateLicenseTask_0_4_0
                  TargetAssemblyPath="$(TargetPath)"
                  NuSealVersion="1.2.3"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
                  Pems="@(Pem_PrefixPackageId1)"
                  ValidationMode="Warning"
                  ValidationScope="Transitive"
                  />

              </Target>

            </Project>
            """;

        var result = Assets.GenerateTargets(parameters1);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void AppendConsumerAsset_ReturnsMergedContent()
    {
        var input = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>
            </Project>
            """;

        var consumerAssetFile = Path.Combine(_testDirectory, "consumer.props");
        var consumerAsset = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(consumerAssetFile, consumerAsset);

        var expectedContent = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>

                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>

            </Project>
            """;

        var result = Assets.AppendConsumerAsset(input, consumerAssetFile);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void AppendConsumerAsset_ReturnInput_GivenEmptyAsset()
    {
        var input = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>
            </Project>
            """;

        var consumerAssetFile = Path.Combine(_testDirectory, "consumer.props");
        var consumerAsset = """
                <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                </Project>
                """;
        File.WriteAllText(consumerAssetFile, consumerAsset);

        var result = Assets.AppendConsumerAsset(input, consumerAssetFile);

        result.Should().Be(input);
    }

    [Fact]
    public void AppendConsumerAsset_ReturnInput_GivenAssetFileDoesntExist()
    {
        var input = "test content";
        var file = Path.Combine(_testDirectory, "nonexistent.props");

        var result = Assets.AppendConsumerAsset(input, file);

        result.Should().Be(input);
    }

    [Fact]
    public void AppendConsumerAsset_ReturnInput_GivenNullAssetFile()
    {
        var input = "test content";

        var result = Assets.AppendConsumerAsset(input, null);

        result.Should().Be(input);
    }

    [Fact]
    public void RemoveProjectTags_GivenValidContent()
    {
        var input = """
            <Project Sdk=""Microsoft.NET.Sdk"">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;

        var expected = """

                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>

            """;

        var result = Assets.RemoveProjectTags(input, "filename");

        result.Should().Be(expected);
    }

    [Fact]
    public void RemoveProjectTags_ThrowsArgumentException_GivenNoProjectStartTag()
    {
        var input = """
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;

        var action = () =>
        {
            Assets.RemoveProjectTags(input, "filename");
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveProjectTags_ThrowsArgumentException_GivenNoEndTag()
    {
        var input = """
            <Project Sdk=""Microsoft.NET.Sdk"">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            """;

        var action = () =>
        {
            Assets.RemoveProjectTags(input, "filename");
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveProjectTags_ThrowsArgumentException_GivenNoClosingTags()
    {
        var input = """
            <Project Sdk=""Microsoft.NET.Sdk""
                <PropertyGroup
            """;

        var action = () =>
        {
            Assets.RemoveProjectTags(input, "filename");
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveProjectTags_ThrowsArgumentException_GivenOpenAngleBracketButNoProjectTag()
    {
        var input = """
            <This is not a project tag but has an open angle bracket
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            """;

        var action = () =>
        {
            Assets.RemoveProjectTags(input, "filename");
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectLineEnding_ReturnCRLF_GivenCRLF()
    {
        var input = "some\r\ntext";

        var result = Assets.DetectLineEnding(input);

        result.Should().Be("\r\n");
    }

    [Fact]
    public void DetectLineEnding_ReturnLF_GivenLF()
    {
        var input = "some\ntext";

        var result = Assets.DetectLineEnding(input);

        result.Should().Be("\n");
    }

    [Fact]
    public void DetectLineEnding_ReturnLF_GivenNoNewLine()
    {
        var input = "some text";

        var result = Assets.DetectLineEnding(input);

        result.Should().Be("\n");
    }

    private static PemData[] GeneratePemData(params string[] productNames)
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

            return new PemData(productName, publicKeyPem);
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
