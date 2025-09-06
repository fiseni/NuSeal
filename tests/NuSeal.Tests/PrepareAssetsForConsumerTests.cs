namespace Tests;

public class PrepareAssetsForConsumerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _assetsDirectory;
    private readonly string _outputDirectory;
    private readonly string _consumerDirectory;

    public PrepareAssetsForConsumerTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        _assetsDirectory = Path.Combine(_testDirectory, "assets");
        _outputDirectory = Path.Combine(_testDirectory, "output");
        _consumerDirectory = Path.Combine(_testDirectory, "consumer");

        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_assetsDirectory);
        Directory.CreateDirectory(_outputDirectory);
        Directory.CreateDirectory(_consumerDirectory);

        CreateTestNuSealFiles();
    }

    [Fact]
    public void ReturnsTrue_GivenValidParameters()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";

        // Act
        bool result = PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: "",
            consumerTargetsFile: "");

        // Assert
        result.Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, $"{consumerPackageId}.props")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, $"{consumerPackageId}.targets")).Should().BeTrue();
    }

    [Fact]
    public void ReplacesVersionInProps_GivenNuSealVersion()
    {
        // Arrange
        var nuSealVersion = "2.3.4";
        var consumerPackageId = "TestConsumer";

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: "",
            consumerTargetsFile: "");

        // Assert
        var propsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.props"));
        propsContent.Should().Contain(nuSealVersion);
        propsContent.Should().NotContain("_NuSealVersion_");
    }

    [Fact]
    public void MergesConsumerPropsContent_GivenConsumerPropsFile()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "consumer.props");
        var consumerPropsContent = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: consumerPropsFile,
            consumerTargetsFile: "");

        // Assert
        var outputPropsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.props"));
        var expectedPropsContent = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>

                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>

            </Project>
            """;

        outputPropsContent.Should().Be(expectedPropsContent);
    }

    [Fact]
    public void MergesConsumerTargetsContent_GivenConsumerTargetsFile()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerTargetsFile = Path.Combine(_consumerDirectory, "consumer.targets");
        var consumerTargetsContent = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <Target Name="TestTarget">
                    <Message Text="Test message" />
                </Target>
            </Project>
            """;

        File.WriteAllText(consumerTargetsFile, consumerTargetsContent);

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: "",
            consumerTargetsFile: consumerTargetsFile);

        // Assert
        var outputTargetsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.targets"));
        var expectedTargetsContent = """
            <Project>
                <Target Name="NuSealCheck">
                    <Message Text="NuSeal is working" />
                </Target>

                <Target Name="TestTarget">
                    <Message Text="Test message" />
                </Target>

            </Project>
            """;

        outputTargetsContent.Should().Be(expectedTargetsContent);
    }

    [Fact]
    public void MergesBothConsumerFiles_GivenBothConsumerFiles()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "consumer.props");
        var consumerTargetsFile = Path.Combine(_consumerDirectory, "consumer.targets");
        var consumerPropsContent = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;
        var consumerTargetsContent = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                <Target Name="TestTarget">
                    <Message Text="Test message" />
                </Target>
            </Project>
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);
        File.WriteAllText(consumerTargetsFile, consumerTargetsContent);

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: consumerPropsFile,
            consumerTargetsFile: consumerTargetsFile);

        // Assert
        var outputPropsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.props"));
        var outputTargetsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.targets"));

        var expectedPropsContent = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>

                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>

            </Project>
            """;
        var expectedTargetsContent = """
            <Project>
                <Target Name="NuSealCheck">
                    <Message Text="NuSeal is working" />
                </Target>

                <Target Name="TestTarget">
                    <Message Text="Test message" />
                </Target>

            </Project>
            """;

        outputPropsContent.Should().Be(expectedPropsContent);
        outputTargetsContent.Should().Be(expectedTargetsContent);
    }

    [Fact]
    public void HandlesEmptyConsumerFiles_GivenEmptyConsumerFiles()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "empty.props");
        var consumerTargetsFile = Path.Combine(_consumerDirectory, "empty.targets");

        File.WriteAllText(consumerPropsFile, "<Project></Project>");
        File.WriteAllText(consumerTargetsFile, "<Project></Project>");

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: consumerPropsFile,
            consumerTargetsFile: consumerTargetsFile);

        // Assert
        var outputPropsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.props"));
        var outputTargetsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.targets"));

        var expectedPropsContent = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>
            </Project>
            """;
        var expectedTargetsContent = """
            <Project>
                <Target Name="NuSealCheck">
                    <Message Text="NuSeal is working" />
                </Target>
            </Project>
            """;

        outputPropsContent.Should().Be(expectedPropsContent);
        outputTargetsContent.Should().Be(expectedTargetsContent);
    }

    [Fact]
    public void HandlesNonExistentConsumerFiles_GivenInvalidFilePaths()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var nonExistentPropsFile = Path.Combine(_consumerDirectory, "nonexistent.props");
        var nonExistentTargetsFile = Path.Combine(_consumerDirectory, "nonexistent.targets");

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: nonExistentPropsFile,
            consumerTargetsFile: nonExistentTargetsFile);

        // Assert
        var outputPropsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.props"));
        var outputTargetsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.targets"));

        var expectedPropsContent = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>1.0.0</NuSealVersion>
                </PropertyGroup>
            </Project>
            """;
        var expectedTargetsContent = """
            <Project>
                <Target Name="NuSealCheck">
                    <Message Text="NuSeal is working" />
                </Target>
            </Project>
            """;

        outputPropsContent.Should().Be(expectedPropsContent);
        outputTargetsContent.Should().Be(expectedTargetsContent);
    }

    [Fact]
    public void RemovesProjectTags_GivenComplexConsumerFiles()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "complex.props");
        var consumerPropsContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);

        // Act
        PrepareAssetsForConsumer.Execute(
            nusealAssetsPath: _assetsDirectory,
            nusealVersion: nuSealVersion,
            outputPath: _outputDirectory,
            consumerPackageId: consumerPackageId,
            consumerPropsFile: consumerPropsFile,
            consumerTargetsFile: "");

        // Assert
        var outputPropsContent = File.ReadAllText(Path.Combine(_outputDirectory, $"{consumerPackageId}.props"));
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
        outputPropsContent.Should().Be(expectedContent);
    }

    [Fact]
    public void ThrowsArgumentException_GivenFileWithNoProjectStartTag()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "no-project-start.props");
        var consumerPropsContent = """
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            </Project>
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);

        // Act
        var action = () =>
        {
            PrepareAssetsForConsumer.Execute(
                nusealAssetsPath: _assetsDirectory,
                nusealVersion: nuSealVersion,
                outputPath: _outputDirectory,
                consumerPackageId: consumerPackageId,
                consumerPropsFile: consumerPropsFile,
                consumerTargetsFile: "");
        };

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowsArgumentException_GivenFileWithProjectStartTagButNoEndTag()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "no-end-tag.props");
        var consumerPropsContent = """
            <Project Sdk=""Microsoft.NET.Sdk"">
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);

        // Act
        var action = () =>
        {
            PrepareAssetsForConsumer.Execute(
                nusealAssetsPath: _assetsDirectory,
                nusealVersion: nuSealVersion,
                outputPath: _outputDirectory,
                consumerPackageId: consumerPackageId,
                consumerPropsFile: consumerPropsFile,
                consumerTargetsFile: "");
        };

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowsArgumentException_GivenFileWithProjectStartTagButNoClosingTags()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "no-end-tag.props");
        var consumerPropsContent = """
            <Project Sdk=""Microsoft.NET.Sdk""
                <PropertyGroup
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);

        // Act
        var action = () =>
        {
            PrepareAssetsForConsumer.Execute(
                nusealAssetsPath: _assetsDirectory,
                nusealVersion: nuSealVersion,
                outputPath: _outputDirectory,
                consumerPackageId: consumerPackageId,
                consumerPropsFile: consumerPropsFile,
                consumerTargetsFile: "");
        };

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThrowsArgumentException_GivenFileWithOpenAngleBracketButNoProjectTag()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "no-project-tag.props");
        var consumerPropsContent = """
            <This is not a project tag but has an open angle bracket
                <PropertyGroup>
                    <TestProperty>TestValue</TestProperty>
                </PropertyGroup>
            """;

        File.WriteAllText(consumerPropsFile, consumerPropsContent);

        // Act
        var action = () =>
        {
            PrepareAssetsForConsumer.Execute(
                nusealAssetsPath: _assetsDirectory,
                nusealVersion: nuSealVersion,
                outputPath: _outputDirectory,
                consumerPackageId: consumerPackageId,
                consumerPropsFile: consumerPropsFile,
                consumerTargetsFile: "");
        };

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    private void CreateTestNuSealFiles()
    {
        // Create NuSeal.Direct.props with placeholder for version
        var propsContent = """
            <Project>
                <PropertyGroup>
                    <NuSealVersion>_NuSealVersion_</NuSealVersion>
                </PropertyGroup>
            </Project>
            """;

        // Create NuSeal.Direct.targets
        var targetsContent = """
            <Project>
                <Target Name="NuSealCheck">
                    <Message Text="NuSeal is working" />
                </Target>
            </Project>
            """;

        File.WriteAllText(Path.Combine(_assetsDirectory, "NuSeal.Direct.props"), propsContent);
        File.WriteAllText(Path.Combine(_assetsDirectory, "NuSeal.Direct.targets"), targetsContent);
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
