namespace Tests;

public sealed class ConsumerParametersTests
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
        parameters.TargetCondition.Should().BeEmpty();
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
        parameters.TargetCondition.Should().Be("Condition=\"'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'\"");
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
    public void Constructor_AddsAllowedLicenseConditions()
    {
        var pem = new PemData("Product", "public-key")
        {
            AllowedLicenses = ["AGPL-3.0", "MIT"]
        };

        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: [pem],
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: null,
            validationScope: "transitive");

        parameters.TargetCondition.Should().Be(
            "Condition=\"('$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe') And '$(ProductLicense)' != 'AGPL-3.0' And '$(ProductLicense)' != 'MIT'\"");
    }

    [Fact]
    public void Constructor_AddsAllowedLicenseConditions_GivenMultiplePems()
    {
        var pem1 = new PemData("Product1", "public-key")
        {
            AllowedLicenses = ["AGPL-3.0", "MIT"]
        };
        var pem2 = new PemData("Product2", "another-public-key")
        {
            AllowedLicenses = ["GPL-3.0"]
        };
        var pem3 = new PemData("Product3", "yet-another-public-key");

        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: [pem1, pem2, pem3],
            condition: null,
            propsFile: null,
            targetsFile: null,
            validationMode: null,
            validationScope: "transitive");

        parameters.TargetCondition.Should().Be(
            "Condition=\"('$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe') And '$(Product1License)' != 'AGPL-3.0' And '$(Product1License)' != 'MIT' And '$(Product2License)' != 'GPL-3.0'\"");
    }

    [Fact]
    public void Constructor_AddsAllowedLicenseConditionsToConsumerCondition()
    {
        var pem = new PemData("Product", "public-key")
        {
            AllowedLicenses = ["AGPL-3.0", "MIT"]
        };

        var parameters = new ConsumerParameters(
            nuSealAssetsPath: "path/to/assets",
            nuSealVersion: "1.0.0",
            outputPath: "path/to/output",
            packageId: "Package.Id",
            assemblyName: "Assembly",
            pems: [pem],
            condition: "'$(Configuration)' == 'Release'",
            propsFile: null,
            targetsFile: null,
            validationMode: null,
            validationScope: null);

        parameters.TargetCondition.Should().Be(
            "Condition=\"('$(Configuration)' == 'Release') And '$(ProductLicense)' != 'AGPL-3.0' And '$(ProductLicense)' != 'MIT'\"");
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var nuSealAssetsPath = "path/to/assets";
        var nuSealVersion = "1.2.3";
        var outputPath = "path/to/output";
        var packageId = "Prefix.PackageId";
        var assemblyName = "AssemblyName";
        var pems = new[] { new PemData("Product", "public-key") };
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
