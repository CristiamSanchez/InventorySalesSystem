using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Entities;

public sealed class Supplier
{
    private Supplier()
    {
        Name = string.Empty;
    }

    public Supplier(
        string name,
        string? contactName = null,
        string? identificationNumber = null,
        EmailAddress? email = null,
        string? phone = null,
        string? address = null,
        DateTimeOffset? createdAtUtc = null)
        : this(Guid.NewGuid(), name, contactName, identificationNumber, email, phone, address, isActive: true, createdAtUtc)
    {
    }

    public Supplier(
        Guid id,
        string name,
        string? contactName = null,
        string? identificationNumber = null,
        EmailAddress? email = null,
        string? phone = null,
        string? address = null,
        bool isActive = true,
        DateTimeOffset? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Supplier ID cannot be empty.", nameof(id));
        }

        Id = id;
        Name = NormalizeName(name);
        ContactName = NormalizeContactName(contactName);
        IdentificationNumber = NormalizeIdentificationNumber(identificationNumber);
        Email = email;
        Phone = NormalizePhone(phone);
        Address = NormalizeAddress(address);
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string? ContactName { get; private set; }

    public string? IdentificationNumber { get; private set; }

    public EmailAddress? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public void UpdateDetails(
        string name,
        string? contactName = null,
        string? identificationNumber = null,
        EmailAddress? email = null,
        string? phone = null,
        string? address = null)
    {
        var normalizedName = NormalizeName(name);
        var normalizedContactName = NormalizeContactName(contactName);
        var normalizedIdentification = NormalizeIdentificationNumber(identificationNumber);
        var normalizedPhone = NormalizePhone(phone);
        var normalizedAddress = NormalizeAddress(address);

        Name = normalizedName;
        ContactName = normalizedContactName;
        IdentificationNumber = normalizedIdentification;
        Email = email;
        Phone = normalizedPhone;
        Address = normalizedAddress;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentException("Supplier name cannot exceed 200 characters.", nameof(name));
        }

        return trimmed;
    }

    private static string? NormalizeContactName(string? contactName)
    {
        if (string.IsNullOrWhiteSpace(contactName))
        {
            return null;
        }

        var trimmed = contactName.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentException("Contact name cannot exceed 200 characters.", nameof(contactName));
        }

        return trimmed;
    }

    private static string? NormalizeIdentificationNumber(string? identificationNumber)
    {
        if (string.IsNullOrWhiteSpace(identificationNumber))
        {
            return null;
        }

        var trimmed = identificationNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > 50)
        {
            throw new ArgumentException("Identification number cannot exceed 50 characters.", nameof(identificationNumber));
        }

        return trimmed;
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        if (trimmed.Length > 30)
        {
            throw new ArgumentException("Phone number cannot exceed 30 characters.", nameof(phone));
        }

        return trimmed;
    }

    private static string? NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var trimmed = address.Trim();
        if (trimmed.Length > 300)
        {
            throw new ArgumentException("Address cannot exceed 300 characters.", nameof(address));
        }

        return trimmed;
    }
}
