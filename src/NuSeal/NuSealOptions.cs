namespace NuSeal;

internal class NuSealOptions
{
    public bool IsProtected { get; set; } = false;
    public NuSealValidationBehavior ValidationBehavior { get; set; } = NuSealValidationBehavior.Error;
    public NuSealTransitiveBehavior TransitiveBehavior { get; set; } = NuSealTransitiveBehavior.Enabled;
}
