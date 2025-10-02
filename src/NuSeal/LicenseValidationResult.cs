namespace NuSeal;

/// <summary>
/// Represents the result of a license validation.
/// </summary>
internal enum LicenseValidationResult
{
    /// <summary>
    /// The license is valid.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// The license has expired but is still within the grace period.
    /// </summary>
    ExpiredWithinGracePeriod = 10,

    /// <summary>
    /// The license has expired and the grace period has also expired.
    /// </summary>
    ExpiredOutsideGracePeriod = 20,

    /// <summary>
    /// The license is invalid (wrong product, wrong format, etc.).
    /// </summary>
    Invalid = 100
}
