NuSeal provides the infrastructure for creating and validating NuGet package licenses. The validation occurs during build time (offline), preventing unauthorized use of your packages. It's designed to be generic while allowing each product package to set its own public key and license policies.

<strong>Packages:</strong>

1. [NuSeal](https://www.nuget.org/packages/NuSeal) - Core package that validates licenses during build time (`netstandard2.0` library)
2. [NuSeal.Generator](https://www.nuget.org/packages/NuSeal.Generator) - Helper package for generating RSA key pairs and licenses (`net8.0` library)

## Table of Contents
- [TL;DR](#tldr)
- [For Package Authors](#for-package-authors)
  - [1. Create RSA Key Pairs](#1-create-rsa-key-pairs)
  - [2. Create Licenses for Users](#2-create-licenses-for-users)
  - [3. Protect Your NuGet Package](#3-protect-your-nuget-package)
- [For End Users](#for-end-users)
- [NuSeal Default Behavior](#nuseal-default-behavior)
- [NuSeal Customization Options](#nuseal-customization-options)
  - [1. Validation Mode](#1-validation-mode)
  - [2. Validation Scope](#2-validation-scope)

## TL;DR

- Authors create RSA key pairs in PEM format. They may create them using `NuSeal.Generator` package.
- Authors create licenses for their users using `NuSeal.Generator` package. License files are named `YourProductName.license`.
- Authors install the `NuSeal` package in their NuGet package to protect it.
- Authors embed the public key in their NuGet package. The PEM file is named `YourProductName.nuseal.pem`.
- End users obtain a license file and place it anywhere in their project directory tree.

## For Package Authors

### 1. Create RSA Key Pairs

Package authors first need to create public/private key pairs. You can use the `NuSeal.Generator` package for this.

```xml
<ItemGroup>
  <PackageReference Include="NuSeal.Generator" Version="0.3.0" />
</ItemGroup>
```

Then generate the keys.

```csharp
var keys = NuSeal.RsaKeyGenerator.GeneratePem();
File.WriteAllText("private_key.pem", keys.PrivateKey);
File.WriteAllText("YourProductName.nuseal.pem", keys.PublicKey);
```

Keep the private key secure and confidential, as it will be used to sign licenses.

### 2. Create Licenses for Users

Once you have the key pair, you can create licenses for your product:

```csharp
var license = NuSeal.License.Create(new()
{
    PrivateKeyPem = keys.PrivateKey,
    ProductName = "YourProductName",
    SubscriptionId = "00000000-0000-0000-0000-000000000000",
    ClientId = "00000000-0000-0000-0000-000000000000",
    Edition = "Free",
    Issuer = "YourCompany",
    Audience = "NuSeal",
    StartDate = DateTimeOffset.UtcNow,
    ExpirationDate = DateTimeOffset.UtcNow.AddYears(1)
});

// Save the license to a file
File.WriteAllText("YourProductName.license", license);
```

Parameters explained:
- privateKeyPem - Your private RSA key in PEM format
- productName - Unique identifier of your product associated with this license. It might be the package name if this license is intended only for this package; or it might be a bundle name if the license is associated with group of packages. <strong>Important: this name is used for both the public key filename and license filename. It must be alphanumeric and should not contain dots (`.`).</strong>
- subscriptionId - Unique identifier for the customer subscription
- clientId - Unique identifier for the customer or user
- edition - Edition of your product (e.g., "Free", "Professional", "Enterprise")
- issuer - Your company or organization name
- audience - Intended audience for the license (e.g., "NuSeal")
- startDate - When the license becomes valid
- expirationDate - When the license expires

### 3. Protect Your NuGet Package

To protect your NuGet package, add the `NuSeal` package as a dependency:

```xml
<ItemGroup>
  <PackageReference Include="NuSeal" Version="0.3.0" />
</ItemGroup>
```

Then, add your public key as an embedded resource. The file should be named `YourProductName.nuseal.pem`:

```xml
<ItemGroup>
  <EmbeddedResource Include="YourProductName.nuseal.pem" />
</ItemGroup>
```

The package authors may include more than one pem file. It's a common practice that authors provide licenses for a single package or a bundle of packages. In this case, the author may include multiple pem files. Even if you use the same private key to create licenses for multiple products, and the public pem is the same, you still need to embed pem file per product.

```xml
<ItemGroup>
  <EmbeddedResource Include="YourProductName.nuseal.pem" />
  <EmbeddedResource Include="YourBundleName.nuseal.pem" />
</ItemGroup>
```

NuSeal will try to find and validate the license against all embedded public keys. At least one valid license is required to pass the validation.

## For End Users

End users of your protected NuGet package need to:

1. Obtain a license file from you (the package author)
2. Place the license file in one of these locations:
   - In the solution/repository root directory.
   - Anywhere in the directory tree.

The license file should be named `YourProductName.license`. <strong>Important:</strong> Avoid checking the license file into source control to prevent leaks.

## NuSeal Default Behavior

The default behavior of NuSeal is as follows.

- License validation flows to all projects in the dependency chain, including both direct and transitive consumers of your protected package.
- Considering the transitive behavior, the license validation is constrained to only executable assemblies. This is determined by checking whether the OutputType of the project is `Exe` or `WinExe`, or the SDK is `Microsoft.NET.Sdk.Web`.
- If no license is found, the build fails with an error.
- The license is validated against the following criteria:
  - The license is signed with the private key corresponding to the embedded public key
  - The license has not expired
  - The `product` claim in the license matches the product name associated with the public key

## NuSeal Customization Options

The authors can customize the default behavior and adjust the policies to fit their needs. Currently, there are two options available which can be set via MSBuild properties in the project file.

### 1. Validation Mode
It alters the behavior when no valid license is found.
  - `Error` (default): The build fails with an error if no valid license is found.
  - `Warning`: The build emits a warning if no valid license is found, but continues.

```xml
<PropertyGroup>
  <NuSealValidationMode>Warning</NuSealValidationMode>
</PropertyGroup>
```

### 2. Validation Scope
Depending on the nature of the library and the business model, authors may want a different strategy where only direct consumers (not transitive ones) are required to have a license.
  - `Transitive` (default): License validation flows to all projects in the dependency chain, including both direct and transitive consumers.
  - `Direct`: This option will change the behavior as follows:
    - Only projects that directly consume your package will be validated for licenses.
    - The project can be of any type. It's not constrained to executable assemblies.

```xml
<PropertyGroup>
    <NuSealValidationScope>Direct</NuSealValidationScope>
</PropertyGroup>
```

For authors that are packing build assets in their package please [read the instructions here](https://github.com/fiseni/NuSeal/issues/12).

## Give a Star! :star:
If you like or are using this project please give it a star. Thanks!
