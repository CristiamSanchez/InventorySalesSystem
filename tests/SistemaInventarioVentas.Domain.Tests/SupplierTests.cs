using SistemaInventarioVentas.Domain.Entities;
using SistemaInventarioVentas.Domain.ValueObjects;

namespace SistemaInventarioVentas.Domain.Tests;

public sealed class SupplierTests
{
    [Fact]
    public void Constructor_WithValidRequiredFields_SucceedsAndDefaultsToActive()
    {
        var before = DateTimeOffset.UtcNow;
        var supplier = new Supplier("Global Logistics Inc.");
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.Equal("Global Logistics Inc.", supplier.Name);
        Assert.True(supplier.IsActive);
        Assert.Null(supplier.ContactName);
        Assert.Null(supplier.IdentificationNumber);
        Assert.Null(supplier.Email);
        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Address);
        Assert.InRange(supplier.CreatedAtUtc, before, after);
    }

    [Fact]
    public void Constructor_WithAllFields_TrimsAndNormalizesInputs()
    {
        var timestamp = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
        var email = new EmailAddress("  orders@globallogistics.com  ");

        var supplier = new Supplier(
            "  Global Logistics Inc.  ",
            "  John Smith  ",
            "  tax-98765-sup  ",
            email,
            "  +1 (555) 987-6543  ",
            "  456 Industrial Blvd, Suite 200  ",
            timestamp);

        Assert.Equal("Global Logistics Inc.", supplier.Name);
        Assert.Equal("John Smith", supplier.ContactName);
        Assert.Equal("TAX-98765-SUP", supplier.IdentificationNumber);
        Assert.Same(email, supplier.Email);
        Assert.NotNull(supplier.Email);
        Assert.Equal("orders@globallogistics.com", supplier.Email.Value);
        Assert.Equal("+1 (555) 987-6543", supplier.Phone);
        Assert.Equal("456 Industrial Blvd, Suite 200", supplier.Address);
        Assert.True(supplier.IsActive);
        Assert.Equal(timestamp, supplier.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_WithWhitespaceOptionalFields_NormalizesToNull()
    {
        var supplier = new Supplier(
            "Acme Supply",
            "   ",
            "   ",
            null,
            "   ",
            "   ");

        Assert.Null(supplier.ContactName);
        Assert.Null(supplier.IdentificationNumber);
        Assert.Null(supplier.Email);
        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Address);
    }

    [Fact]
    public void Constructor_WithRehydrationParameters_InitializesCorrectly()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
        var email = new EmailAddress("vendor@supply.com");

        var supplier = new Supplier(
            id,
            "Supply Direct",
            "Jane Doe",
            "SUP-001",
            email,
            "555-1234",
            "100 Distribution Way",
            isActive: false,
            timestamp);

        Assert.Equal(id, supplier.Id);
        Assert.Equal("Supply Direct", supplier.Name);
        Assert.Equal("Jane Doe", supplier.ContactName);
        Assert.Equal("SUP-001", supplier.IdentificationNumber);
        Assert.Same(email, supplier.Email);
        Assert.Equal("555-1234", supplier.Phone);
        Assert.Equal("100 Distribution Way", supplier.Address);
        Assert.False(supplier.IsActive);
        Assert.Equal(timestamp, supplier.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyGuid()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Supplier(Guid.Empty, "Acme Supply"));
        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingOrBlankName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Supplier(name!));
    }

    [Fact]
    public void Constructor_RejectsOversizedName()
    {
        var oversizedName = new string('A', 201);
        Assert.Throws<ArgumentException>(() => new Supplier(oversizedName));

        var maxName = new string('A', 200);
        var supplier = new Supplier(maxName);
        Assert.Equal(maxName, supplier.Name);
    }

    [Fact]
    public void Constructor_RejectsOversizedContactName()
    {
        var oversizedContact = new string('C', 201);
        Assert.Throws<ArgumentException>(() => new Supplier("Supplier Corp", contactName: oversizedContact));

        var maxContact = new string('C', 200);
        var supplier = new Supplier("Supplier Corp", contactName: maxContact);
        Assert.Equal(maxContact, supplier.ContactName);
    }

    [Fact]
    public void Constructor_RejectsOversizedIdentificationNumber()
    {
        var oversizedId = new string('X', 51);
        Assert.Throws<ArgumentException>(() => new Supplier("Supplier Corp", identificationNumber: oversizedId));

        var maxId = new string('X', 50);
        var supplier = new Supplier("Supplier Corp", identificationNumber: maxId);
        Assert.Equal(maxId.ToUpperInvariant(), supplier.IdentificationNumber);
    }

    [Fact]
    public void Constructor_RejectsOversizedPhone()
    {
        var oversizedPhone = new string('1', 31);
        Assert.Throws<ArgumentException>(() => new Supplier("Supplier Corp", phone: oversizedPhone));

        var maxPhone = new string('1', 30);
        var supplier = new Supplier("Supplier Corp", phone: maxPhone);
        Assert.Equal(maxPhone, supplier.Phone);
    }

    [Fact]
    public void Constructor_RejectsOversizedAddress()
    {
        var oversizedAddress = new string('A', 301);
        Assert.Throws<ArgumentException>(() => new Supplier("Supplier Corp", address: oversizedAddress));

        var maxAddress = new string('A', 300);
        var supplier = new Supplier("Supplier Corp", address: maxAddress);
        Assert.Equal(maxAddress, supplier.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@vendor.com")]
    [InlineData("vendor@@vendor.com")]
    [InlineData("vendor name@vendor.com")]
    public void EmailAddress_RejectsInvalidFormat(string invalidEmail)
    {
        Assert.Throws<ArgumentException>(() => new EmailAddress(invalidEmail));
    }

    [Fact]
    public void ActivateAndDeactivate_ChangeSupplierStatus()
    {
        var supplier = new Supplier("Acme Supply");
        Assert.True(supplier.IsActive);

        supplier.Deactivate();
        Assert.False(supplier.IsActive);

        supplier.Activate();
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesAndNormalizesProperties()
    {
        var supplier = new Supplier("Original Supplier");
        var newEmail = new EmailAddress("support@newsup.com");

        supplier.UpdateDetails(
            "  Updated Supplier  ",
            "  Alice Agent  ",
            "  new-tax-789  ",
            newEmail,
            "  +1 (555) 000-1111  ",
            "  789 Commercial Rd  ");

        Assert.Equal("Updated Supplier", supplier.Name);
        Assert.Equal("Alice Agent", supplier.ContactName);
        Assert.Equal("NEW-TAX-789", supplier.IdentificationNumber);
        Assert.Same(newEmail, supplier.Email);
        Assert.Equal("+1 (555) 000-1111", supplier.Phone);
        Assert.Equal("789 Commercial Rd", supplier.Address);
    }

    [Fact]
    public void UpdateDetails_WithWhitespaceOptionalFields_SetsPropertiesToNull()
    {
        var supplier = new Supplier(
            "Supply Corp",
            "Contact Person",
            "TAX-999",
            new EmailAddress("vendor@corp.com"),
            "123-4567",
            "100 Street");

        supplier.UpdateDetails("Supply Corp Updated", "   ", "   ", null, "   ", "   ");

        Assert.Equal("Supply Corp Updated", supplier.Name);
        Assert.Null(supplier.ContactName);
        Assert.Null(supplier.IdentificationNumber);
        Assert.Null(supplier.Email);
        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Address);
    }

    [Fact]
    public void UpdateDetails_PreservesIdentityAndCreationTimestamp()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var supplier = new Supplier(id, "Initial Supplier", isActive: true, createdAtUtc: timestamp);

        supplier.UpdateDetails("Renamed Supplier");

        Assert.Equal(id, supplier.Id);
        Assert.Equal(timestamp, supplier.CreatedAtUtc);
        Assert.True(supplier.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_RejectsBlankNameWithoutAlteringState(string? blankName)
    {
        var supplier = new Supplier("Original Supplier", contactName: "Bob");

        Assert.ThrowsAny<ArgumentException>(() => supplier.UpdateDetails(blankName!));
        Assert.Equal("Original Supplier", supplier.Name);
        Assert.Equal("Bob", supplier.ContactName);
    }

    [Fact]
    public void UpdateDetails_RejectsOversizedFieldsWithoutAlteringState()
    {
        var supplier = new Supplier("Original Supplier", "Bob Contact", "TAX-ORIG", phone: "123", address: "Main St");

        Assert.Throws<ArgumentException>(() => supplier.UpdateDetails(new string('A', 201)));
        Assert.Throws<ArgumentException>(() => supplier.UpdateDetails("Name", contactName: new string('C', 201)));
        Assert.Throws<ArgumentException>(() => supplier.UpdateDetails("Name", identificationNumber: new string('X', 51)));
        Assert.Throws<ArgumentException>(() => supplier.UpdateDetails("Name", phone: new string('1', 31)));
        Assert.Throws<ArgumentException>(() => supplier.UpdateDetails("Name", address: new string('A', 301)));

        Assert.Equal("Original Supplier", supplier.Name);
        Assert.Equal("Bob Contact", supplier.ContactName);
        Assert.Equal("TAX-ORIG", supplier.IdentificationNumber);
        Assert.Equal("123", supplier.Phone);
        Assert.Equal("Main St", supplier.Address);
    }
}
