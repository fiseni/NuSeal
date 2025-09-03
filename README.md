<img align="left" src="logo.png" width="120" height="120">

&nbsp; [![NuGet](https://img.shields.io/nuget/v/NuSeal.svg)](https://www.nuget.org/packages/NuSeal)

&nbsp; [![Full Build](https://github.com/fiseni/NuSeal/actions/workflows/build.yml/badge.svg)](https://github.com/fiseni/NuSeal/actions/workflows/build.yml)

&nbsp; 

---
# NuSeal

NuSeal provides infrastructure for creating and validating NuGet package licenses. The validation occurs during build time, preventing unauthorized usage of your packages. It's designed to be generic while allowing each product package to set its own public key and license policies.

## Overview

NuSeal consists of two main packages:

1. **NuSeal** - Core package that validates licenses during build time
2. **NuSeal.Generator** - Helper package for generating RSA key pairs and licenses

## Usage Guide

### For Package Authors

#### 1. Create RSA Key Pairs

Package authors first need to create public/private key pairs. You can use the `NuSeal.Generator` package for this:

```xml
<ItemGroup>
  <PackageReference Include="NuSeal.Generator" Version="0.0.1" />
</ItemGroup>
```

Then generate the keys:

```csharp
var keys = NuSeal.RsaKeyGenerator.GeneratePem();
File.WriteAllText("private_key.pem", keys.PrivateKey);
File.WriteAllText("public_key.pem", keys.PublicKey);
```

Keep the private key secure and confidential, as it will be used to sign licenses.

#### 2. Create Licenses for Users

Once you have the key pair, you can create licenses for your product:

```csharp
var license = NuSealLicense.Create(
    privateKeyPem: keys.PrivateKey,
    subscriptionId: "00000000-0000-0000-0000-000000000000",
    productName: "YourProduct",
    edition: "Free",
    issuer: "YourCompany",
    startDate: DateTimeOffset.UtcNow,
    expirationDate: DateTimeOffset.UtcNow.AddYears(1));

// Save the license to a file
File.WriteAllText("YourProduct.license", license);
```

Parameters explained:
- **privateKeyPem**: Your private RSA key in PEM format
- **subscriptionId**: Unique identifier for the customer subscription
- **productName**: Name of your product (important - this name is used for both the public key filename and license filename)
- **edition**: Edition of your product (e.g., "Free", "Professional", "Enterprise")
- **issuer**: Your company or organization name
- **startDate**: When the license becomes valid
- **expirationDate**: When the license expires

Note: The `productName` must be alphanumeric and should not contain dots (`.`) as it's used for file naming.

#### 3. Protect Your NuGet Package

To protect your NuGet package, add the `NuSeal` package as a dependency:

```xml
<ItemGroup>
  <PackageReference Include="NuSeal" Version="0.0.1-alpha3" />
</ItemGroup>
```

Then, add your public key as an embedded resource. The file should be named `YourProduct.nuseal.pem`:

```xml
<ItemGroup>
  <EmbeddedResource Include="YourProduct.nuseal.pem" />
</ItemGroup>
```

### For End Users

End users of your protected NuGet package need to:

1. Obtain a license file from you (the package author)
2. Place the license file in one of these locations:
   - Same directory as the application executable
   - Root of the solution or repository

The license file should be named `YourProduct.license` where `YourProduct` matches the `productName` parameter used when creating the license.

## How It Works

1. NuSeal adds a build task that runs during the build process
2. The task identifies assemblies marked with `NuSealProtectedAttribute`
3. For each protected assembly, it extracts the embedded public key
4. It searches for a matching license file in the project directory tree
5. The license is validated against the public key
6. If no valid license is found, the build fails with an error

## Give a Star! :star:
If you like or are using this project please give it a star. Thanks!
