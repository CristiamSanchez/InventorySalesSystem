using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class CustomerTests
{
    [Fact]
    public void Constructor_WithValidRequiredFields_SucceedsAndDefaultsToActive()
    {
        var before = DateTimeOffset.UtcNow;
        var customer = new Customer("Acme Corp");
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Acme Corp", customer.Name);
        Assert.True(customer.IsActive);
        Assert.Null(customer.IdentificationNumber);
        Assert.Null(customer.Email);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Address);
        Assert.InRange(customer.CreatedAtUtc, before, after);
    }

    [Fact]
    public void Constructor_WithAllFields_TrimsAndNormalizesInputs()
    {
        var timestamp = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
        var email = new EmailAddress("  contact@acme.com  ");

        var customer = new Customer(
            "  Acme Corporation  ",
            "  tax-12345-xyz  ",
            email,
            "  +1 (555) 123-4567  ",
            "  123 Main St, Suite 100  ",
            timestamp);

        Assert.Equal("Acme Corporation", customer.Name);
        Assert.Equal("TAX-12345-XYZ", customer.IdentificationNumber);
        Assert.Same(email, customer.Email);
        Assert.NotNull(customer.Email);
        Assert.Equal("contact@acme.com", customer.Email.Value);
        Assert.Equal("+1 (555) 123-4567", customer.Phone);
        Assert.Equal("123 Main St, Suite 100", customer.Address);
        Assert.True(customer.IsActive);
        Assert.Equal(timestamp, customer.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_WithWhitespaceOptionalFields_NormalizesToNull()
    {
        var customer = new Customer(
            "Acme Corp",
            "   ",
            null,
            "   ",
            "   ");

        Assert.Null(customer.IdentificationNumber);
        Assert.Null(customer.Email);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Address);
    }

    [Fact]
    public void Constructor_WithRehydrationParameters_InitializesCorrectly()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        var email = new EmailAddress("info@acme.com");

        var customer = new Customer(
            id,
            "Acme Rehydrated",
            "ID-999",
            email,
            "555-9999",
            "742 Evergreen Terrace",
            isActive: false,
            timestamp);

        Assert.Equal(id, customer.Id);
        Assert.Equal("Acme Rehydrated", customer.Name);
        Assert.Equal("ID-999", customer.IdentificationNumber);
        Assert.Same(email, customer.Email);
        Assert.Equal("555-9999", customer.Phone);
        Assert.Equal("742 Evergreen Terrace", customer.Address);
        Assert.False(customer.IsActive);
        Assert.Equal(timestamp, customer.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() => new Customer(Guid.Empty, "Acme Corp"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingOrBlankName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Customer(name!));
    }

    [Fact]
    public void Constructor_RejectsOversizedName()
    {
        var oversizedName = new string('A', 201);
        Assert.Throws<ArgumentException>(() => new Customer(oversizedName));

        var maxName = new string('A', 200);
        var customer = new Customer(maxName);
        Assert.Equal(maxName, customer.Name);
    }

    [Fact]
    public void Constructor_RejectsOversizedIdentificationNumber()
    {
        var oversizedId = new string('X', 51);
        Assert.Throws<ArgumentException>(() => new Customer("Acme", identificationNumber: oversizedId));

        var maxId = new string('X', 50);
        var customer = new Customer("Acme", identificationNumber: maxId);
        Assert.Equal(maxId.ToUpperInvariant(), customer.IdentificationNumber);
    }

    [Fact]
    public void Constructor_RejectsOversizedPhone()
    {
        var oversizedPhone = new string('1', 31);
        Assert.Throws<ArgumentException>(() => new Customer("Acme", phone: oversizedPhone));

        var maxPhone = new string('1', 30);
        var customer = new Customer("Acme", phone: maxPhone);
        Assert.Equal(maxPhone, customer.Phone);
    }

    [Fact]
    public void Constructor_RejectsOversizedAddress()
    {
        var oversizedAddress = new string('A', 301);
        Assert.Throws<ArgumentException>(() => new Customer("Acme", address: oversizedAddress));

        var maxAddress = new string('A', 300);
        var customer = new Customer("Acme", address: maxAddress);
        Assert.Equal(maxAddress, customer.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("customer@@example.com")]
    [InlineData("customer name@example.com")]
    public void EmailAddress_RejectsInvalidFormat(string invalidEmail)
    {
        Assert.Throws<ArgumentException>(() => new EmailAddress(invalidEmail));
    }

    [Fact]
    public void ActivateAndDeactivate_ChangeCustomerStatus()
    {
        var customer = new Customer("Acme Corp");
        Assert.True(customer.IsActive);

        customer.Deactivate();
        Assert.False(customer.IsActive);

        customer.Activate();
        Assert.True(customer.IsActive);
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesAndNormalizesProperties()
    {
        var customer = new Customer("Original Name");
        var newEmail = new EmailAddress("updated@acme.com");

        customer.UpdateDetails(
            "  Updated Name  ",
            "  new-id-123  ",
            newEmail,
            "  555-0100  ",
            "  456 Oak Avenue  ");

        Assert.Equal("Updated Name", customer.Name);
        Assert.Equal("NEW-ID-123", customer.IdentificationNumber);
        Assert.Same(newEmail, customer.Email);
        Assert.Equal("555-0100", customer.Phone);
        Assert.Equal("456 Oak Avenue", customer.Address);
    }

    [Fact]
    public void UpdateDetails_WithWhitespaceOptionalFields_SetsPropertiesToNull()
    {
        var customer = new Customer(
            "Acme",
            "TAX-1",
            new EmailAddress("a@b.com"),
            "123",
            "Street");

        customer.UpdateDetails("Acme Updated", "   ", null, "   ", "   ");

        Assert.Equal("Acme Updated", customer.Name);
        Assert.Null(customer.IdentificationNumber);
        Assert.Null(customer.Email);
        Assert.Null(customer.Phone);
        Assert.Null(customer.Address);
    }

    [Fact]
    public void UpdateDetails_PreservesIdentityAndCreationTimestamp()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero);
        var customer = new Customer(id, "Initial Customer", isActive: true, createdAtUtc: timestamp);

        customer.UpdateDetails("Renamed Customer");

        Assert.Equal(id, customer.Id);
        Assert.Equal(timestamp, customer.CreatedAtUtc);
        Assert.True(customer.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_RejectsBlankNameWithoutAlteringState(string? blankName)
    {
        var customer = new Customer("Original Name");

        Assert.ThrowsAny<ArgumentException>(() => customer.UpdateDetails(blankName!));
        Assert.Equal("Original Name", customer.Name);
    }

    [Fact]
    public void UpdateDetails_RejectsOversizedFieldsWithoutAlteringState()
    {
        var customer = new Customer("Original Name", "ORIG-ID", phone: "123", address: "Main St");

        Assert.Throws<ArgumentException>(() => customer.UpdateDetails(new string('A', 201)));
        Assert.Throws<ArgumentException>(() => customer.UpdateDetails("Name", identificationNumber: new string('X', 51)));
        Assert.Throws<ArgumentException>(() => customer.UpdateDetails("Name", phone: new string('1', 31)));
        Assert.Throws<ArgumentException>(() => customer.UpdateDetails("Name", address: new string('A', 301)));

        Assert.Equal("Original Name", customer.Name);
        Assert.Equal("ORIG-ID", customer.IdentificationNumber);
        Assert.Equal("123", customer.Phone);
        Assert.Equal("Main St", customer.Address);
    }
}
