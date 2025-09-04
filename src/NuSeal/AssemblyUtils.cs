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

    internal static bool IsNuSealProtected(AssemblyDefinition assembly)
    {
        if (assembly.MainModule.HasCustomAttributes is false)
        {
            return false;
        }

        foreach (var attribute in assembly.CustomAttributes)
        {
            if (attribute.AttributeType.FullName == typeof(NuSealProtectedAttribute).FullName)
            {
                return true;
            }
        }

        return false;
    }
}
