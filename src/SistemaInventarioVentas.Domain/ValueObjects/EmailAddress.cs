namespace SistemaInventarioVentas.Domain.ValueObjects;

public sealed record EmailAddress
{
    public EmailAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalizedValue = value.Trim();
        var separatorIndex = normalizedValue.IndexOf('@');
        if (separatorIndex <= 0 ||
            separatorIndex != normalizedValue.LastIndexOf('@') ||
            separatorIndex == normalizedValue.Length - 1 ||
            normalizedValue.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("A valid email address is required.", nameof(value));
        }

        Value = normalizedValue.ToLowerInvariant();
    }

    public string Value { get; }
}
