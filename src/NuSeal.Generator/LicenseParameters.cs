using System;

namespace NuSeal;

public record LicenseParameters
{
    public required string PrivateKeyPem { get; init; }
    public required string ProductName { get; init; }
    public string Issuer { get; init; } = "NuSeal";
    public string Audience { get; init; } = "NuSeal";
    public string SubscriptionId { get; init; } = Guid.Empty.ToString();
    public string? ClientId { get; init; }
    public string? Edition { get; init; }
    public DateTimeOffset StartDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpirationDate { get; init; } = DateTimeOffset.UtcNow.AddYears(1);
}
