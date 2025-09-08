using Mono.Cecil;
using System.Reflection;

namespace Tests;

public class LicenseValidationTests : IDisposable
{
    private readonly RsaPemPair _rsaPemPair;
    private readonly TestLogger _log;
    private readonly string _tempDir;
    private readonly string _mainAssemblyPath;

    public LicenseValidationTests()
    {
        _rsaPemPair = RsaKeyGenerator.GeneratePem();
        _log = new TestLogger();
        _tempDir = Path.Combine(Path.GetTempPath(), $"NuSealTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _mainAssemblyPath = Path.Combine(_tempDir, "MainApp.dll");
        File.WriteAllText(_mainAssemblyPath, "Dummy content");
    }

    [Fact]
    public void ReturnsTrue_GivenDllWithoutProtection()
    {
        var dll = CreateNonProtectedDll();

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll], NuSealValidationScope.Transitive);

        result.Should().BeTrue();
        _log.Messages.Should().BeEmpty();
        _log.Warnings.Should().BeEmpty();
        _log.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_GivenDllWithDifferentValidationScope()
    {
        var dll = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error);

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll], NuSealValidationScope.Direct);

        result.Should().BeTrue();
        _log.Messages.Should().BeEmpty();
        _log.Warnings.Should().BeEmpty();
        _log.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenDllWithoutPems()
    {
        var dll = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: false);

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll], NuSealValidationScope.Transitive);

        result.Should().BeTrue();
        _log.Messages.Should().NotBeEmpty();
        _log.Warnings.Should().BeEmpty();
        _log.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenDllWithoutValidLicenseAndErrorMode()
    {
        var productName = "TestProduct";
        var dll = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: true,
            productName: productName);

        // No license file created

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll], NuSealValidationScope.Transitive);

        result.Should().BeFalse();
        _log.Messages.Should().BeEmpty();
        _log.Warnings.Should().BeEmpty();
        _log.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsWarning_GivenDllWithoutValidLicenseAndWarningMode()
    {
        var productName = "TestProduct";
        var dll = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Warning,
            includePems: true,
            productName: productName);

        // No license file created

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll], NuSealValidationScope.Transitive);

        result.Should().BeTrue();
        _log.Messages.Should().BeEmpty();
        _log.Warnings.Should().NotBeEmpty();
        _log.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsInfo_GivenExceptionDuringDllProcessing()
    {
        // Create an invalid DLL that will cause exception during processing
        var invalidDllPath = Path.Combine(_tempDir, "Invalid.dll");
        File.WriteAllText(invalidDllPath, "Not a valid DLL");

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [invalidDllPath], NuSealValidationScope.Transitive);

        result.Should().BeTrue();
        _log.Messages.Should().NotBeEmpty();
        _log.Warnings.Should().BeEmpty();
        _log.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_GivenDllWithValidLicense()
    {
        var productName = "TestProduct";
        var dll = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: true,
            productName: productName);

        CreateValidLicense(productName);

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll], NuSealValidationScope.Transitive);

        result.Should().BeTrue();
        _log.Messages.Should().BeEmpty();
        _log.Errors.Should().BeEmpty();
        _log.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsTrue_LogsWarning_GivenMultipleDllsWithValidAndInvalidLicenses()
    {
        var productName1 = "Product1";
        var productName2 = "Product2";
        var productName3 = "Product3";

        // Create three DLLs with different configurations
        var dll1 = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: true,
            productName: productName1,
            fileName: "Library1.dll");

        var dll2 = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Warning,
            includePems: true,
            productName: productName2,
            fileName: "Library2.dll");

        var dll3 = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: true,
            productName: productName3,
            fileName: "Library3.dll");

        // Create valid licenses only for products 1 and 3
        CreateValidLicense(productName1);
        CreateValidLicense(productName3);

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll1, dll2, dll3], NuSealValidationScope.Transitive);

        result.Should().BeTrue("since the DLL with missing license has Warning mode");
        _log.Messages.Should().BeEmpty();
        _log.Warnings.Should().NotBeEmpty();
        _log.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnsFalse_LogsError_GivenMultipleDllsWithOneErrorModeMissingLicense()
    {
        var productName1 = "Product1";
        var productName2 = "Product2";

        // Create two DLLs with different configurations
        var dll1 = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: true,
            productName: productName1,
            fileName: "Library1.dll");

        var dll2 = CreateProtectedDll(
            validationScope: NuSealValidationScope.Transitive,
            validationMode: NuSealValidationMode.Error,
            includePems: true,
            productName: productName2,
            fileName: "Library2.dll");

        // We're not creating valid licenses - all should fail validation

        var result = LicenseValidation.Execute(_log, _mainAssemblyPath, [dll1, dll2], NuSealValidationScope.Transitive);

        result.Should().BeFalse();
        _log.Messages.Should().BeEmpty();
        _log.Warnings.Should().BeEmpty();
        _log.Errors.Should().NotBeEmpty();
    }

    private void CreateValidLicense(string productName)
    {
        var licenseParams = new LicenseParameters
        {
            ProductName = productName,
            PrivateKeyPem = _rsaPemPair.PrivateKey,
        };
        var license = License.Create(licenseParams);
        var licenseFilePath = Path.Combine(_tempDir, $"{productName}.license");
        File.WriteAllText(licenseFilePath, license);
    }

    private string CreateNonProtectedDll(string fileName = "Library.dll")
    {
        var dllPath = Path.Combine(_tempDir, fileName);

        var assemblyDef = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0)),
            "TestModule",
            ModuleKind.Dll);

        // Save the assembly
        assemblyDef.Write(dllPath);

        return dllPath;
    }

    private string CreateProtectedDll(
        NuSealValidationScope validationScope,
        NuSealValidationMode validationMode,
        bool includePems = true,
        string productName = "TestProduct",
        string fileName = "Library.dll")
    {
        var dllPath = Path.Combine(_tempDir, fileName);

        // Create a test assembly
        var assemblyDef = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0)),
            "TestModule",
            ModuleKind.Dll);

        // Add NuSealProtectedAttribute
        var protectedAttrCtor = GetAttributeConstructor(typeof(NuSealProtectedAttribute));
        var protectedAttrRef = assemblyDef.MainModule.ImportReference(protectedAttrCtor);
        var protectedAttr = new CustomAttribute(protectedAttrRef);
        assemblyDef.CustomAttributes.Add(protectedAttr);

        // Add NuSealValidationModeAttribute
        var validationModeAttrCtor = GetAttributeConstructor(typeof(NuSealValidationModeAttribute), typeof(string));
        var validationModeAttrRef = assemblyDef.MainModule.ImportReference(validationModeAttrCtor);
        var validationModeAttr = new CustomAttribute(validationModeAttrRef);
        validationModeAttr.ConstructorArguments.Add(
            new CustomAttributeArgument(
                assemblyDef.MainModule.ImportReference(typeof(string)),
                validationMode == NuSealValidationMode.Warning ? "Warning" : "Error"));
        assemblyDef.CustomAttributes.Add(validationModeAttr);

        // Add NuSealValidationScopeAttribute
        var validationScopeAttrCtor = GetAttributeConstructor(typeof(NuSealValidationScopeAttribute), typeof(string));
        var validationScopeAttrRef = assemblyDef.MainModule.ImportReference(validationScopeAttrCtor);
        var validationScopeAttr = new CustomAttribute(validationScopeAttrRef);
        validationScopeAttr.ConstructorArguments.Add(
            new CustomAttributeArgument(
                assemblyDef.MainModule.ImportReference(typeof(string)),
                validationScope == NuSealValidationScope.Direct ? "Direct" : "Transitive"));
        assemblyDef.CustomAttributes.Add(validationScopeAttr);

        // Add PEM resource if needed
        if (includePems)
        {
            var pemContent = _rsaPemPair.PublicKey;
            var resourceName = $"TestCompany.{productName}.nuseal.pem";
            var resource = new EmbeddedResource(
                resourceName,
                Mono.Cecil.ManifestResourceAttributes.Public,
                System.Text.Encoding.UTF8.GetBytes(pemContent));
            assemblyDef.MainModule.Resources.Add(resource);
        }

        // Save the assembly
        assemblyDef.Write(dllPath);

        return dllPath;
    }

    private static MethodReference GetAttributeConstructor(Type attributeType, params Type[] parameterTypes)
    {
        var constructors = attributeType.GetConstructors();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            if (parameters.Length != parameterTypes.Length)
                continue;

            bool match = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return GetMethodReference(constructor);
        }

        throw new InvalidOperationException($"Constructor for {attributeType.FullName} with the specified parameters not found.");
    }

    private static MethodReference GetMethodReference(MethodBase method)
    {
        var declaringType = method.DeclaringType;

        // Create a dummy assembly to import the method reference
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("DummyAssembly", new Version(1, 0)),
            "DummyModule",
            ModuleKind.Dll);

        var typeReference = assembly.MainModule.ImportReference(declaringType);

        if (method is ConstructorInfo constructorInfo)
        {
            return assembly.MainModule.ImportReference(constructorInfo);
        }
        else
        {
            return assembly.MainModule.ImportReference(method as MethodInfo);
        }
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
        }
    }
}
