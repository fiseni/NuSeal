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
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Error);
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Enabled);
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
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Error); // Default
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Enabled); // Default
    }

    [Fact]
    public void SetsWarningValidationBehavior_GivenAssemblyWithWarningAttribute()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationBehaviorAttribute).Name, "Warning");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeFalse(); // Default
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Warning);
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Enabled); // Default
    }

    [Fact]
    public void KeepsDefaultValidationBehavior_GivenAssemblyWithInvalidValidationBehavior()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationBehaviorAttribute).Name, "InvalidValue");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Error); // Default unchanged
    }

    [Fact]
    public void SetsDisabledTransitiveBehavior_GivenAssemblyWithDisabledAttribute()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealTransitiveBehaviorAttribute).Name, "disable");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeFalse(); // Default
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Error); // Default
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Disabled);
    }

    [Fact]
    public void KeepsDefaultTransitiveBehavior_GivenAssemblyWithInvalidTransitiveBehavior()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealTransitiveBehaviorAttribute).Name, "InvalidValue");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Enabled); // Default unchanged
    }

    [Fact]
    public void CombinesAllAttributes_GivenAssemblyWithMultipleAttributes()
    {
        // Arrange
        AddCustomAttribute(_testAssembly, typeof(NuSealProtectedAttribute).Name);
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationBehaviorAttribute).Name, "Warning");
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealTransitiveBehaviorAttribute).Name, "disable");

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.IsProtected.Should().BeTrue();
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Warning);
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Disabled);
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
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Error); // Default
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Enabled); // Default
    }

    [Fact]
    public void HandlesCaseInsensitively_GivenAttributeValuesWithDifferentCase()
    {
        // Arrange
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealValidationBehaviorAttribute).Name, "warning"); // lowercase
        AddCustomAttributeWithValue(_testAssembly, typeof(NuSealTransitiveBehaviorAttribute).Name, "DISABLE"); // uppercase

        // Act
        var result = AssemblyUtils.ExtractOptions(_testAssembly);

        // Assert
        result.Should().NotBeNull();
        result.ValidationBehavior.Should().Be(NuSealValidationBehavior.Warning);
        result.TransitiveBehavior.Should().Be(NuSealTransitiveBehavior.Disabled);
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
