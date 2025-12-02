<img align="left" src="logo.png" width="120" height="120">

&nbsp; [![NuGet](https://img.shields.io/nuget/v/NuSeal.svg)](https://www.nuget.org/packages/NuSeal)

&nbsp; [![Actions Status](https://github.com/fiseni/NuSeal/actions/workflows/ci.yml/badge.svg)](https://github.com/fiseni/NuSeal/actions/workflows/ci.yml)

&nbsp; 

---
NuSeal provides the infrastructure for creating and validating NuGet package licenses. The validation occurs during build time (offline), preventing unauthorized use of your packages. It's designed to be generic while allowing each author to set their own public key and license policies.

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
  - [3. Validation Condition](#3-validation-condition)
  - [4. Output Path](#4-output-path)
  - [5. Merging custom assets](#5-merging-custom-assets)
  - [6. Disable packing assets](#6-disable-packing-assets)

## TL;DR

- Authors create RSA key pairs in PEM format. They may create them using `NuSeal.Generator` package.
- Authors create licenses for their users using `NuSeal.Generator` package. License files are named `YourProductName.lic`.
- Authors install the `NuSeal` package in their NuGet package to protect it.
- Authors add `NuSealPem` item providing the path to the public key PEM and the name of the product.
- End users obtain a license file and place it anywhere in their project directory tree.

## For Package Authors

### 1. Create RSA Key Pairs

Package authors first need to create public/private key pairs. You can use the `NuSeal.Generator` package for this.

```xml
<ItemGroup>
  <PackageReference Include="NuSeal.Generator" Version="0.4.2" />
</ItemGroup>
```

Then generate the keys.

```csharp
var keys = NuSeal.RsaKeyGenerator.GeneratePem();
File.WriteAllText("private_key.pem", keys.PrivateKey);
File.WriteAllText("public_key.pem", keys.PublicKey);
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
    ExpirationDate = DateTimeOffset.UtcNow.AddYears(1),
    GracePeriodInDays = 30,
    AdditionalClaims = []
});

// Save the license to a file
File.WriteAllText("YourProductName.lic", license);
```

Parameters explained:
- privateKeyPem - Your private RSA key in PEM format
- productName - Unique identifier of your product associated with this license. It might be the package name if this license is intended only for this package; or it might be a bundle name if the license is associated with group of packages. <strong>Important: this name is used while defining the `NuSealPem` item and as a license filename.</strong>
- subscriptionId - Unique identifier for the customer subscription
- clientId - Unique identifier for the customer or user
- edition - Edition of your product (e.g., "Free", "Professional", "Enterprise")
- issuer - Your company or organization name
- audience - Intended audience for the license (e.g., "NuSeal")
- startDate - When the license becomes valid
- expirationDate - When the license expires
- gracePeriodInDays - Number of days after expiration during which the license is still considered valid. It will emit a warning instead of an error during validation.
- additionalClaims - Any additional claims you want to include in the license

### 3. Protect Your NuGet Package

To protect your NuGet package, add the `NuSeal` package as a dependency:

```xml
<ItemGroup>
  <PackageReference Include="NuSeal" Version="0.4.2" />
</ItemGroup>
```

Then, add a `NuSealPem` item providing the path to the public key PEM and the name of the product:

```xml
<ItemGroup>
  <NuSealPem Include="public_key.pem" ProductName="YourProductName" />
</ItemGroup>
```

It's a common practice that authors provide licenses for a single package or a bundle of packages. In this case, you may add multiple items. You may use the same or a different PEM file per product.

```xml
<ItemGroup>
  <NuSealPem Include="public_key.pem" ProductName="YourProductName" />
  <NuSealPem Include="public_key.pem" ProductName="YourBundleName" />
</ItemGroup>
```

NuSeal will try to find and validate the license against all specified products. At least one valid license is required to pass the validation.

## For End Users

End users of your protected NuGet package need to:

1. Obtain a license file from you (the package author)
2. Place the license file in one of these locations:
   - In the solution/repository root directory.
   - Anywhere in the directory tree.

The license file should be named `YourProductName.lic`. <strong>Important:</strong> Avoid checking the license file into source control to prevent leaks.

## NuSeal Default Behavior

The default behavior of NuSeal is as follows.

- The `YourPackageId.props` and `YourPackageId.targets` assets are generated in the build output path.
- The generated assets are packed into the NuGet package under the `build` folder.
- License validation is executed for direct consumers of the protected package.
- If no license is found, the build fails with an error.
- The license is validated against the following criteria:
  - The license has valid lifetime
  - The license is signed with the private key corresponding to the specified public key in `NuSealPem`
  - The `product` claim in the license matches the product name specified in `NuSealPem`

## NuSeal Customization Options

The authors can customize the default behavior and adjust the policies to fit their needs.

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
Depending on the nature of the library and the business model, authors may want a different strategy where even transitive consumers are required to have a license.
  - `Direct` (default): The assets are packed only to `build` directory. Only projects that directly consume the protected package will be validated for licenses.
  - `Transitive`: The assets are packed to `buildTransitive` and `build` directories. The `build` is necessary to support projects using `packages.config`. The assets will flow to all consumers, direct and transitive. For this scope, to avoid cluttering the build for large solutions, we're constraining the validation to only executable assemblies.

```xml
<PropertyGroup>
    <NuSealValidationScope>Transitive</NuSealValidationScope>
</PropertyGroup>
```

### 3. Validation Condition
The generated target, depending on the validation scope, may or may not include a condition.
  - `Direct` (default): No condition is applied. All projects that directly consume the protected package will be validated for licenses.
  - `Transitive`: The target includes a condition to only validate executable assemblies.
    ```xml
    Condition="'$(OutputType)' == 'Exe' Or '$(OutputType)' == 'WinExe'"
    ```
The authors may alter this behavior and specify their custom condition as follows. If defined, it will be applied regardless of the scope.

```xml
<PropertyGroup>
  <NuSealCondition>"'#(OutputType)' == 'Exe' Or '#(OutputType)' == 'WinExe'"</NuSealCondition>
</PropertyGroup>
```

Note that `#()` is used instead of `$()`. Since we need to preserve the original condition as literal, and avoid variable evaluation; the `$` and `@` characters should not be used. Use the `#` character instead, and we'll do the replacement during asset generation.
- Use `##` for `@`
- Use `#` for `$`

### 4. Output Path

By default, the assets `YourPackageId.props` and `YourPackageId.targets` are generated in the build output path; the value of `$(OutputPath)`.
If the authors want to generate the assets in a different location, they can specify an output path as follows.

```xml
<PropertyGroup>
  <NuSealOutputPath>YOUR_DESIRED_OUTPUT_PATH</NuSealOutputPath>
</PropertyGroup>
```

### 5. Merging custom assets

By default, once the `YourPackageId.props` and `YourPackageId.targets` assets are generated, we add items to pack them in the NuGet package. We're packing them in `build` directory, and in case of `Transitive` scope in `buildTransitive` directory as well.

```xml
<None Include="$(OutputPath)\$(PackageId).props" Pack="true" PackagePath="build\$(PackageId).props" Visible="false"/>
<None Include="$(OutputPath)\$(PackageId).targets" Pack="true" PackagePath="build\$(PackageId).targets" Visible="false"/>
```

If the author is already packing assets to their NuGet package, they can provide them to NuSeal. We'll do the merging and pack the merged assets.

```xml
<PropertyGroup>
    <NuSealIncludePropsFile>build\YourPackageId.props</NuSealIncludePropsFile>
    <NuSealIncludeTargetsFile>build\YourPackageId.targets</NuSealIncludeTargetsFile>
</PropertyGroup>
```

### 6. Disable packing assets

Authors may have a different strategy for generating NuGet packages (e.g. they use nuspec files), have complex workflows or simply want to manually pack the assets. In that case they may disable packing assets altogether. We'll just generate the assets in the output path, and it's up to the authors to pack or further process them.

```xml
<PropertyGroup>
  <NuSealPackAssets>disable</NuSealPackAssets>
</PropertyGroup>
```

## Give a Star! :star:
If you like or are using this project please give it a star. Thanks!
