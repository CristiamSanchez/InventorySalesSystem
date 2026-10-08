namespace SistemaInventarioVentas.Domain.ValueObjects;

public sealed class PasswordHash
{
    public PasswordHash(string encodedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedValue);
        EncodedValue = encodedValue;
    }

    public string EncodedValue { get; }

    public override string ToString() => "[REDACTED]";
}
