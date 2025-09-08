using Mono.Cecil;

namespace Tests;

public class AssemblyUtils_ExtractOptionsTests : IDisposable
{
    private readonly AssemblyDefinition _testAssembly;

    public AssemblyUtils_ExtractOptionsTests()
    {
        // Create a simple assembly definition for testing
        var assemblyName = new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0));
        _testAssembly = AssemblyDefinition.CreateAssembly(
            assemblyName,
            "TestModule",
            ModuleKind.Dll);
    }

    [Fact]
    public void ReturnsDefaultOptions_GivenAssemblyWithNoAttributes()
    {
        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeFalse();
        result.ValidationMode.Should().Be(NuSealValidationMode.Error);
        result.ValidationScope.Should().Be(NuSealValidationScope.Transitive);
    }

    [Fact]
    public void SetsIsProtected_GivenAssemblyWithProtectedAttribute()
    {
        // Arrange
        AddCustomAttribute(_testAssembly, typeof(NuSealProtectedAttribute).Name);

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeTrue();
        result.ValidationMode.Should().Be(NuSealValidationMode.Error); // Default
        result.ValidationScope.Should().Be(NuSealValidationScope.Transitive); // Default
    }

    [Fact]
    public void SetsWarningValidationMode_GivenAssemblyWithWarningModeAttribute()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationModeAttribute).Name, "Warning");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeFalse(); // Default
        result.ValidationMode.Should().Be(NuSealValidationMode.Warning);
        result.ValidationScope.Should().Be(NuSealValidationScope.Transitive); // Default
    }

    [Fact]
    public void KeepsDefaultValidationMode_GivenAssemblyWithInvalidValidationMode()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationModeAttribute).Name, "InvalidValue");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.ValidationMode.Should().Be(NuSealValidationMode.Error); // Default unchanged
    }

    [Fact]
    public void SetsDirectValidationScope_GivenAssemblyWithDirectScopeAttribute()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationScopeAttribute).Name, "Direct");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeFalse(); // Default
        result.ValidationMode.Should().Be(NuSealValidationMode.Error); // Default
        result.ValidationScope.Should().Be(NuSealValidationScope.Direct);
    }

    [Fact]
    public void KeepsDefaultValidationScope_GivenAssemblyWithInvalidValidationScope()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationScopeAttribute).Name, "InvalidValue");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.ValidationScope.Should().Be(NuSealValidationScope.Transitive); // Default unchanged
    }

    [Fact]
    public void CombinesAllAttributes_GivenAssemblyWithMultipleAttributes()
    {
        // Arrange
        AddCustomAttribute(_testAssembly, typeof(NuSealProtectedAttribute).Name);
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationModeAttribute).Name, "Warning");
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationScopeAttribute).Name, "Direct");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeTrue();
        result.ValidationMode.Should().Be(NuSealValidationMode.Warning);
        result.ValidationScope.Should().Be(NuSealValidationScope.Direct);
    }

    [Fact]
    public void IgnoresUnrelatedAttributes_GivenAssemblyWithMixedAttributes()
    {
        // Arrange
        AddCustomAttribute(_testAssembly, typeof(NuSealProtectedAttribute).Name);
        AddCustomAttribute(_testAssembly, "SomeOtherAttribute");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeTrue();
        result.ValidationMode.Should().Be(NuSealValidationMode.Error); // Default
        result.ValidationScope.Should().Be(NuSealValidationScope.Transitive); // Default
    }

    [Fact]
    public void HandlesCaseInsensitively_GivenAttributeValuesWithDifferentCase()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationModeAttribute).Name, "warning"); // lowercase
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationScopeAttribute).Name, "DIRECT"); // uppercase

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.ValidationMode.Should().Be(NuSealValidationMode.Warning);
        result.ValidationScope.Should().Be(NuSealValidationScope.Direct);
    }

    private static void AddCustomAttribute(AssemblyDefinition assembly, string attributeTypeName)
    {
        var moduleRef = new ModuleReference("System.Runtime");
        assembly.MainModule.ModuleReferences.Add(moduleRef);

        var attributeType = new TypeReference(
            "NuSeal",
            attributeTypeName,
            assembly.MainModule,
            moduleRef);

        var attributeCtor = new MethodReference(
            ".ctor",
            assembly.MainModule.TypeSystem.Void,
            attributeType);

        attributeCtor.Parameters.Clear();
        attributeCtor.HasThis = true;

        var attribute = new CustomAttribute(attributeCtor);
        assembly.CustomAttributes.Add(attribute);
    }

    private static void AddCustomAttributeWithValue(AssemblyDefinition assembly, string attributeTypeName, string value)
    {
        var moduleRef = new ModuleReference("System.Runtime");
        assembly.MainModule.ModuleReferences.Add(moduleRef);

        var attributeType = new TypeReference(
            "NuSeal",
            attributeTypeName,
            assembly.MainModule,
            moduleRef);

        var attributeCtor = new MethodReference(
            ".ctor",
            assembly.MainModule.TypeSystem.Void,
            attributeType);

        var stringType = assembly.MainModule.TypeSystem.String;
        attributeCtor.Parameters.Add(new ParameterDefinition(stringType));
        attributeCtor.HasThis = true;

        var attribute = new CustomAttribute(attributeCtor);
        attribute.ConstructorArguments.Add(
            new CustomAttributeArgument(stringType, value));

        assembly.CustomAttributes.Add(attribute);
    }

    public void Dispose()
    {
        _testAssembly?.Dispose();
    }
}
