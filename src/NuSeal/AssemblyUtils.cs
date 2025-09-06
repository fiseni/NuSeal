using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuSeal;

internal class AssemblyUtils
{
    private const string _pemFileSuffix = "nuseal.pem";
    private static readonly char[] _resourceNameDelimiter = new[] { '.' };

    internal static List<PemData> ExtractPems(AssemblyDefinition assembly)
    {
        var pems = new List<PemData>();

        if (assembly.MainModule.HasResources is false)
        {
            return pems;
        }

        foreach (var resource in assembly.MainModule.Resources)
        {
            if (resource is EmbeddedResource embeddedResource
                && embeddedResource.Name.EndsWith(_pemFileSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var parts = embeddedResource.Name.Split(_resourceNameDelimiter, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    continue;
                }
                var productName = parts[parts.Length - 3]; // Get the part before "nuseal.pem"

                using var stream = embeddedResource.GetResourceStream();
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();

                var pem = new PemData(productName, content);
                pems.Add(pem);
            }
        }

        return pems;
    }

    internal static NuSealOptions ExtractOptions(AssemblyDefinition assembly)
    {
        var options = new NuSealOptions();

        if (assembly.HasCustomAttributes is false)
        {
            return options;
        }

        foreach (var attribute in assembly.CustomAttributes)
        {
            if (attribute.AttributeType.FullName == typeof(NuSealProtectedAttribute).FullName)
            {
                options.IsProtected = true;
            }
            else if (attribute.AttributeType.FullName == typeof(NuSealValidationBehaviorAttribute).FullName)
            {
                if (attribute.ConstructorArguments.Count == 1
                    && attribute.ConstructorArguments[0].Value is string behavior)
                {
                    if (behavior.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                    {
                        options.ValidationBehavior = NuSealValidationBehavior.Warning;
                    }
                }
            }
            else if (attribute.AttributeType.FullName == typeof(NuSealTransitiveBehaviorAttribute).FullName)
            {
                if (attribute.ConstructorArguments.Count == 1
                    && attribute.ConstructorArguments[0].Value is string behavior)
                {
                    if (behavior.Equals("disable", StringComparison.OrdinalIgnoreCase))
                    {
                        options.TransitiveBehavior = NuSealTransitiveBehavior.Disabled;
                    }
                }
            }
        }

        return options;
    }
}
