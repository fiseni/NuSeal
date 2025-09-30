using System;

namespace NuSeal;

internal class NuSealOptions
{
    public NuSealValidationMode ValidationMode { get; }
    public NuSealValidationScope ValidationScope { get; }

    public NuSealOptions(
        string? validationMode,
        string? validationScope)
    {
        ValidationMode = string.Equals(validationMode, "Warning", StringComparison.OrdinalIgnoreCase)
            ? NuSealValidationMode.Warning
            : NuSealValidationMode.Error;

        ValidationScope = string.Equals(validationScope, "Direct", StringComparison.OrdinalIgnoreCase)
            ? NuSealValidationScope.Direct
            : NuSealValidationScope.Transitive;
    }
}
