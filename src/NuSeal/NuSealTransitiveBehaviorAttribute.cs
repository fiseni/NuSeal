using System;

namespace NuSeal;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class NuSealTransitiveBehaviorAttribute : Attribute
{
    public string Value { get; }

    public NuSealTransitiveBehaviorAttribute(string value)
    {
        Value = value;
    }
}
