using System;

namespace NuSeal;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class NuSealValidationModeAttribute : Attribute
{
    public string Value { get; }

    public NuSealValidationModeAttribute(string value)
    {
        Value = value;
    }
}
