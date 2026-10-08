namespace SistemaInventarioVentas.Domain.ValueObjects;

public sealed record ProductIdentifier
{
    public ProductIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
}
