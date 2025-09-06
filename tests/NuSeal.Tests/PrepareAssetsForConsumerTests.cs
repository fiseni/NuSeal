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
        var consumerPropsContent = "<PropertyGroup><TestProperty>TestValue</TestProperty></PropertyGroup>";
        var fileContent = $"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">{consumerPropsContent}</Project>";

        File.WriteAllText(consumerPropsFile, fileContent);

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
        outputPropsContent.Should().Contain(consumerPropsContent);
    }

    [Fact]
    public void MergesConsumerTargetsContent_GivenConsumerTargetsFile()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerTargetsFile = Path.Combine(_consumerDirectory, "consumer.targets");
        var consumerTargetsContent = "<Target Name=\"TestTarget\"><Message Text=\"Test message\" /></Target>";
        var fileContent = $"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">{consumerTargetsContent}</Project>";

        File.WriteAllText(consumerTargetsFile, fileContent);

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
        outputTargetsContent.Should().Contain(consumerTargetsContent);
    }

    [Fact]
    public void MergesBothConsumerFiles_GivenBothConsumerFiles()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "consumer.props");
        var consumerTargetsFile = Path.Combine(_consumerDirectory, "consumer.targets");
        var consumerPropsContent = "<PropertyGroup><TestProperty>TestValue</TestProperty></PropertyGroup>";
        var consumerTargetsContent = "<Target Name=\"TestTarget\"><Message Text=\"Test message\" /></Target>";

        File.WriteAllText(consumerPropsFile, $"<Project>{consumerPropsContent}</Project>");
        File.WriteAllText(consumerTargetsFile, $"<Project>{consumerTargetsContent}</Project>");

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

        outputPropsContent.Should().Contain(consumerPropsContent);
        outputTargetsContent.Should().Contain(consumerTargetsContent);
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
        var propsOutputFile = Path.Combine(_outputDirectory, $"{consumerPackageId}.props");
        var targetsOutputFile = Path.Combine(_outputDirectory, $"{consumerPackageId}.targets");

        File.Exists(propsOutputFile).Should().BeTrue();
        File.Exists(targetsOutputFile).Should().BeTrue();
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
        var propsOutputFile = Path.Combine(_outputDirectory, $"{consumerPackageId}.props");
        var targetsOutputFile = Path.Combine(_outputDirectory, $"{consumerPackageId}.targets");

        File.Exists(propsOutputFile).Should().BeTrue();
        File.Exists(targetsOutputFile).Should().BeTrue();
    }

    [Fact]
    public void RemovesProjectTags_GivenComplexConsumerFiles()
    {
        // Arrange
        var nuSealVersion = "1.0.0";
        var consumerPackageId = "TestConsumer";
        var consumerPropsFile = Path.Combine(_consumerDirectory, "complex.props");
        var consumerPropsContent = """
            <Project Sdk=""Microsoft.NET.Sdk"">
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
        outputPropsContent.Should().Contain(@"<TestProperty>TestValue</TestProperty>");
        outputPropsContent.Should().NotContain(@"<Project Sdk=""Microsoft.NET.Sdk"">");
        // Should only have one Project tag (from the original NuSeal.Direct.props)
        outputPropsContent.Count(c => c == '<').Should().Be(outputPropsContent.Count(c => c == '>'));
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
                <Target Name=""NuSealCheck"">
                    <Message Text=""NuSeal is working"" />
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
