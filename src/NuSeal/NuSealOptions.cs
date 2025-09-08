namespace NuSeal;

internal class NuSealOptions
{
    public bool IsProtected { get; set; } = false;
    public NuSealValidationMode ValidationMode { get; set; } = NuSealValidationMode.Error;
    public NuSealTransitiveBehavior TransitiveBehavior { get; set; } = NuSealTransitiveBehavior.Enabled;
}
