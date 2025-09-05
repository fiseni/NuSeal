using System;

namespace NuSeal;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class NuSealValidationBehaviorAttribute : Attribute
{
    public string Value { get; }

    public NuSealValidationBehaviorAttribute(string value)
    {
        Value = value;
    }
}
