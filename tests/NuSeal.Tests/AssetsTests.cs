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
            propsFile: null,
            targetsFile: null,
            validationMode: null,
            validationScope: null);

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <PropertyGroup>
                <NuSealAssembly_PrefixPackageId1>$([MSBuild]::NormalizePath('$(NugetPackageRoot)', 'nuseal', '1.2.3', 'tasks', 'netstandard2.0', 'NuSeal.dll'))</NuSealAssembly_PrefixPackageId1>
              </PropertyGroup>

              <UsingTask
                TaskName="NuSeal.ValidateLicenseTask"
                AssemblyFile="$(NuSealAssembly_PrefixPackageId1)"
                TaskFactory="TaskHostFactory" />

            </Project>
            """;

        var result = Assets.GenerateProps(parameters);

        result.Should().Be(expectedContent);
    }

    [Fact]
    public void GenerateTargets_ReturnUniqueAssetWithNoCondition_GivenDirectScope()
    {
        var parameters1 = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            propsFile: null,
            targetsFile: null,
            validationMode: "Warning",
            validationScope: "Direct");

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      DependsOnTargets="ResolvePackageAssets"
                      AfterTargets="AfterBuild"
                      >

                <NuSeal.ValidateLicenseTask
                  PackageReferences="@(PackageReference)"
                  ResolvedCompileFileDefinitions="@(ResolvedCompileFileDefinitions)"
                  MainAssemblyPath="$(TargetPath)"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
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
    public void GenerateTargets_ReturnUniqueAssetWithCondition_GivenTransitiveScope()
    {
        var parameters1 = new ConsumerParameters(
            nuSealAssetsPath: "path/to/nuseal/assets",
            nuSealVersion: "1.2.3",
            outputPath: _testDirectory,
            packageId: "Prefix.PackageId1",
            assemblyName: "Assembly1",
            propsFile: null,
            targetsFile: null,
            validationMode: "Warning",
            validationScope: "Transitive");

        var expectedContent = $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

              <Target Name="NuSealValidateLicense_PrefixPackageId1"
                      DependsOnTargets="ResolvePackageAssets"
                      AfterTargets="AfterBuild"
                      Condition="'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe' Or '$(MSBuildProjectSdk)' == 'Microsoft.NET.Sdk.Web'">

                <NuSeal.ValidateLicenseTask
                  PackageReferences="@(PackageReference)"
                  ResolvedCompileFileDefinitions="@(ResolvedCompileFileDefinitions)"
                  MainAssemblyPath="$(TargetPath)"
                  ProtectedPackageId="Prefix.PackageId1"
                  ProtectedAssemblyName="Assembly1"
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
