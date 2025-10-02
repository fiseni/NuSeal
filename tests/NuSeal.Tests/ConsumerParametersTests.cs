namespace Tests;

public class ConsumerParametersTests
{
    [Fact]
    public void Constructor_SetsDefaultOptions_GivenNull()
    {
        var parameters = new ConsumerParameters(
            "path/to/assets",
            "1.0.0", 
            "path/to/output",
            "Package.Id",
            "Assembly",
            Array.Empty<PemData>(),
            null,
            null,
            null,
            null);

        parameters.ValidationMode.Should().Be("Error");
        parameters.ValidationScope.Should().Be("Direct");
        parameters.Options.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_HandlesNullPropsAndTargetsFiles()
    {
        var parameters = new ConsumerParameters(
            "path/to/assets",
            "1.0.0",
            "path/to/output", 
            "Package.Id",
            "Assembly",
            Array.Empty<PemData>(),
            null,
            null,
            "Error",
            "Transitive");

        parameters.PropsFile.Should().BeNull();
        parameters.TargetsFile.Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsPropsAndTargetsToNull_GivenEmptyStrings()
    {
        var parameters = new ConsumerParameters(
            "path/to/assets",
            "1.0.0",
            "path/to/output",
            "Package.Id",
            "Assembly",
            Array.Empty<PemData>(),
            "",
            "  ",
            "Error",
            "Transitive");

        parameters.PropsFile.Should().BeNull();
        parameters.TargetsFile.Should().BeNull();
    }

    [Fact]
    public void Suffix_RemovesDots_FromPackageId()
    {
        var parameters = new ConsumerParameters(
            "path/to/assets",
            "1.0.0",
            "path/to/output",
            "My.Complex.Package.Id",
            "Assembly",
            Array.Empty<PemData>(),
            null,
            null,
            "Error",
            "Transitive");

        parameters.Suffix.Should().Be("MyComplexPackageId");
    }

    [Theory]
    [InlineData("Warning", "Direct", "Warning", "Direct")]
    [InlineData("warning", "direct", "Warning", "Direct")]
    [InlineData("ERROR", "TRANSITIVE", "Error", "Transitive")]
    [InlineData("Error", "Transitive", "Error", "Transitive")]
    [InlineData("Unknown", "Unknown", "Error", "Direct")]
    public void Constructor_HandlesValidationModeAndScopeCorrectly(
        string inputValidationMode,
        string inputValidationScope,
        string expectedMode,
        string expectedScope)
    {
        var parameters = new ConsumerParameters(
            "path/to/assets",
            "1.0.0",
            "path/to/output",
            "Package.Id",
            "Assembly",
            Array.Empty<PemData>(),
            null,
            null,
            inputValidationMode,
            inputValidationScope);

        parameters.ValidationMode.Should().Be(expectedMode);
        parameters.ValidationScope.Should().Be(expectedScope);
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var nuSealAssetsPath = "path/to/assets";
        var nuSealVersion = "1.2.3";
        var outputPath = "path/to/output";
        var packageId = "Prefix.PackageId";
        var assemblyName = "AssemblyName";
        var pems = new PemData[1];
        var propsFile = "props.props";
        var targetsFile = "targets.targets";
        var validationMode = "Warning";
        var validationScope = "Direct";

        var parameters = new ConsumerParameters(
            nuSealAssetsPath,
            nuSealVersion,
            outputPath,
            packageId,
            assemblyName,
            pems,
            propsFile,
            targetsFile,
            validationMode,
            validationScope);

        parameters.NuSealAssetsPath.Should().Be(nuSealAssetsPath);
        parameters.NuSealVersion.Should().Be(nuSealVersion);
        parameters.OutputPath.Should().Be(outputPath);
        parameters.PackageId.Should().Be(packageId);
        parameters.AssemblyName.Should().Be(assemblyName);
        parameters.Pems.Should().BeSameAs(pems);
        parameters.PropsFile.Should().Be(propsFile);
        parameters.TargetsFile.Should().Be(targetsFile);
        parameters.ValidationMode.Should().Be("Warning");
        parameters.ValidationScope.Should().Be("Direct");
        parameters.Suffix.Should().Be("PrefixPackageId");
        parameters.Options.Should().NotBeNull();
    }
}
