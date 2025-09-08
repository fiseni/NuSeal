using System;

namespace NuSeal;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class NuSealValidationScopeAttribute : Attribute
{
    public string Value { get; }

    public NuSealValidationScopeAttribute(string value)
    {
        Value = value;
    }
}
