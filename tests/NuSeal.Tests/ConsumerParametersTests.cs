namespace Tests;

public class ConsumerParametersTests
{
    [Fact]
    public void Constructor_SetsDefaultOptions_GivenNull()
    {
        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: Array.Empty<PemData>(),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: null,
            validationScope: null);

        parameters.ValidationMode.Should().Be("Error");
        parameters.ValidationScope.Should().Be("Direct");
        parameters.Options.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_HandlesNullPropsAndTargetsFiles()
    {
        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: Array.Empty<PemData>(),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: "Error",
            validationScope: "Transitive");

        parameters.PropsFile.Should().BeNull();
        parameters.TargetsFile.Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsPropsAndTargetsToNull_GivenEmptyStrings()
    {
        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: Array.Empty<PemData>(),
            condition: null,
            propsFile: "",
            targetsFile: "  ",
            validationMode: "Error",
            validationScope: "Transitive");

        parameters.PropsFile.Should().BeNull();
        parameters.TargetsFile.Should().BeNull();
    }

    [Fact]
    public void Suffix_RemovesDots_FromPackageId()
    {
        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "My.Complex.Package.Id",
            assemblyName: "Assembly",
            pems: Array.Empty<PemData>(),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: "Error",
            validationScope: "Transitive");

        parameters.PackageSuffix.Should().Be("MyComplexPackageId");
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
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: Array.Empty<PemData>(),
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: inputValidationMode,
            validationScope: inputValidationScope);

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
        var condition = "'#(OutputType)' == 'Exe' Or '#(OutputType)' == 'WinExe'";
        var propsFile = "props.props";
        var targetsFile = "targets.targets";
        var validationMode = "Warning";
        var validationScope = "Direct";

        var parameters = new ConsumerParameters(
            nuSealAssetsPath: nuSealAssetsPath,
            nuSealVersion: nuSealVersion,
            outputPath: outputPath,
            packageId: packageId,
            assemblyName: assemblyName,
            pems: pems,
            condition: condition,
            propsFile: propsFile,
            targetsFile: targetsFile,
            validationMode: validationMode,
            validationScope: validationScope);

        parameters.NuSealAssetsPath.Should().Be(nuSealAssetsPath);
        parameters.NuSealVersion.Should().Be(nuSealVersion);
        parameters.OutputPath.Should().Be(outputPath);
        parameters.PackageId.Should().Be(packageId);
        parameters.AssemblyName.Should().Be(assemblyName);
        parameters.Pems.Should().BeSameAs(pems);
        parameters.TargetCondition.Should().Be("Condition=\"'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'\"");
        parameters.PropsFile.Should().Be(propsFile);
        parameters.TargetsFile.Should().Be(targetsFile);
        parameters.ValidationMode.Should().Be("Warning");
        parameters.ValidationScope.Should().Be("Direct");
        parameters.PackageSuffix.Should().Be("PrefixPackageId");
        parameters.Options.Should().NotBeNull();
    }
}
