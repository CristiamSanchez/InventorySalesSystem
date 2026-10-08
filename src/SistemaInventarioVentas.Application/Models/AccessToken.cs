namespace SistemaInventarioVentas.Application.Models;

public sealed class AccessToken(string value, DateTimeOffset expiresAtUtc)
{
    public string Value { get; } = string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("An access token is required.", nameof(value))
        : value;

    public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;

    public override string ToString() =>
        $"AccessToken {{ ExpiresAtUtc = {ExpiresAtUtc:O}, Value = [REDACTED] }}";
}
