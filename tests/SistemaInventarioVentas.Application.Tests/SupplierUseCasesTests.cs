using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class SupplierUseCasesTests
{
    private readonly InMemorySupplierRepository _repository = new();
    private SupplierUseCases UseCases => new(_repository);

    [Fact]
    public async Task CreateAsync_WithValidNameOnly_CreatesActiveSupplierAndCallsAdd()
    {
        var result = await UseCases.CreateAsync("  Acme Supplies  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Supplies", result.Value.Name);
        Assert.Null(result.Value.ContactName);
        Assert.Null(result.Value.IdentificationNumber);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Phone);
        Assert.Null(result.Value.Address);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, _repository.AddCount);
        Assert.Contains(result.Value, _repository.Suppliers);
    }

    [Fact]
    public async Task CreateAsync_WithAllValidFields_NormalizesAndPersistsSupplier()
    {
        var createdAt = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

        var result = await UseCases.CreateAsync(
            name: "  Acme Global Supplies  ",
            contactName: "  Jane Doe  ",
            identificationNumber: "  tax-sup-999  ",
            email: "  SALES@ACMESUPPLIES.COM  ",
            phone: "  +1-555-8822  ",
            address: "  789 Industrial Ave  ",
            createdAtUtc: createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Global Supplies", result.Value.Name);
        Assert.Equal("Jane Doe", result.Value.ContactName);
        Assert.Equal("TAX-SUP-999", result.Value.IdentificationNumber);
        Assert.NotNull(result.Value.Email);
        Assert.Equal("sales@acmesupplies.com", result.Value.Email.Value);
        Assert.Equal("+1-555-8822", result.Value.Phone);
        Assert.Equal("789 Industrial Ave", result.Value.Address);
        Assert.True(result.Value.IsActive);
        Assert.Equal(createdAt, result.Value.CreatedAtUtc);
        Assert.Equal(1, _repository.AddCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_RejectsBlankName(string? name)
    {
        var result = await UseCases.CreateAsync(name!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsNameExceeding200Characters()
    {
        var longName = new string('S', 201);

        var result = await UseCases.CreateAsync(longName);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsContactNameExceeding200Characters()
    {
        var longContactName = new string('C', 201);

        var result = await UseCases.CreateAsync("Valid Name", contactName: longContactName);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidEmailFormat()
    {
        var result = await UseCases.CreateAsync("Acme Supplies", email: "not-a-valid-email");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsPhoneExceeding30Characters()
    {
        var longPhone = new string('8', 31);

        var result = await UseCases.CreateAsync("Acme Supplies", phone: longPhone);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsAddressExceeding300Characters()
    {
        var longAddress = new string('A', 301);

        var result = await UseCases.CreateAsync("Acme Supplies", address: longAddress);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsIdentificationNumberExceeding50Characters()
    {
        var longId = new string('1', 51);

        var result = await UseCases.CreateAsync("Acme Supplies", identificationNumber: longId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Identification number cannot exceed 50 characters.", result.Error!.Message);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateIdentificationNumber_ReturnsConflict()
    {
        _repository.Suppliers.Add(new Supplier("Existing Supplier", identificationNumber: "TAX-12345"));

        var result = await UseCases.CreateAsync("New Supplier", identificationNumber: "  tax-12345  ");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.SupplierIdentificationConflict, result.Error!.Code);
        Assert.Equal("A supplier with this identification number already exists.", result.Error!.Message);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_WhenAddAsyncThrowsDuplicateSupplierIdentificationException_ReturnsConflict()
    {
        _repository.SimulateAddDuplicateException = true;

        var result = await UseCases.CreateAsync("New Supplier", identificationNumber: "TAX-NEW");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.SupplierIdentificationConflict, result.Error!.Code);
        Assert.Equal("Simulated duplicate identification conflict.", result.Error!.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsMultipleSuppliersWithoutIdentificationNumber()
    {
        var result1 = await UseCases.CreateAsync("Supplier One", identificationNumber: null);
        var result2 = await UseCases.CreateAsync("Supplier Two", identificationNumber: "   ");

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Null(result1.Value.IdentificationNumber);
        Assert.Null(result2.Value.IdentificationNumber);
        Assert.Equal(2, _repository.AddCount);
        Assert.Equal(2, _repository.Suppliers.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ReturnsInvalidInput()
    {
        var result = await UseCases.GetByIdAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Supplier ID cannot be empty.", result.Error!.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSupplierDoesNotExist_ReturnsNotFound()
    {
        var result = await UseCases.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Supplier was not found.", result.Error!.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSupplierExists_ReturnsSupplier()
    {
        var supplier = new Supplier("Existing Supplier");
        _repository.Suppliers.Add(supplier);

        var result = await UseCases.GetByIdAsync(supplier.Id);

        Assert.True(result.IsSuccess);
        Assert.Same(supplier, result.Value);
    }

    [Fact]
    public async Task ListAsync_WithoutFilter_ReturnsAllSuppliers()
    {
        var supplier1 = new Supplier("Supplier A");
        var supplier2 = new Supplier("Supplier B");
        _repository.Suppliers.AddRange([supplier1, supplier2]);

        var result = await UseCases.ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(supplier1, result.Value);
        Assert.Contains(supplier2, result.Value);
    }

    [Fact]
    public async Task ListAsync_WithIsActiveFilter_FiltersCorrectly()
    {
        var activeSupplier = new Supplier("Active Supplier");
        var inactiveSupplier = new Supplier("Inactive Supplier");
        inactiveSupplier.Deactivate();
        _repository.Suppliers.AddRange([activeSupplier, inactiveSupplier]);

        var activeResult = await UseCases.ListAsync(new SupplierQuery(IsActive: true));
        var inactiveResult = await UseCases.ListAsync(new SupplierQuery(IsActive: false));

        Assert.True(activeResult.IsSuccess);
        Assert.Single(activeResult.Value);
        Assert.Contains(activeSupplier, activeResult.Value);

        Assert.True(inactiveResult.IsSuccess);
        Assert.Single(inactiveResult.Value);
        Assert.Contains(inactiveSupplier, inactiveResult.Value);
    }

    [Theory]
    [InlineData("acme", 1)]
    [InlineData("john", 1)]
    [InlineData("vat-77", 1)]
    [InlineData("nonexistent", 0)]
    public async Task ListAsync_WithSearchTerm_MatchesNameContactNameOrIdentificationNumber(string term, int expectedCount)
    {
        var supplier1 = new Supplier(
            name: "Acme Industrial",
            contactName: "John Smith",
            identificationNumber: "VAT-7788");
        var supplier2 = new Supplier(
            name: "Beta Logistics",
            contactName: "Alice Walker",
            identificationNumber: "TAX-1122");
        _repository.Suppliers.AddRange([supplier1, supplier2]);

        var result = await UseCases.ListAsync(new SupplierQuery(SearchTerm: term));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCount, result.Value.Count);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithEmptyGuid_ReturnsInvalidInput()
    {
        var result = await UseCases.UpdateDetailsAsync(Guid.Empty, "Updated Name");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Supplier ID cannot be empty.", result.Error!.Message);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WhenSupplierDoesNotExist_ReturnsNotFound()
    {
        var result = await UseCases.UpdateDetailsAsync(Guid.NewGuid(), "Updated Name");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Supplier was not found.", result.Error!.Message);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithValidFields_UpdatesAndCallsSave()
    {
        var supplier = new Supplier("Original Name");
        _repository.Suppliers.Add(supplier);

        var result = await UseCases.UpdateDetailsAsync(
            id: supplier.Id,
            name: "  Updated Name  ",
            contactName: "  Jane Smith  ",
            identificationNumber: "  tax-updated-1  ",
            email: "  info@updated.com  ",
            phone: "  555-0101  ",
            address: "  456 New Road  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", supplier.Name);
        Assert.Equal("Jane Smith", supplier.ContactName);
        Assert.Equal("TAX-UPDATED-1", supplier.IdentificationNumber);
        Assert.NotNull(supplier.Email);
        Assert.Equal("info@updated.com", supplier.Email.Value);
        Assert.Equal("555-0101", supplier.Phone);
        Assert.Equal("456 New Road", supplier.Address);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithInvalidInput_LeavesSupplierUnchangedAndReturnsInvalidInput()
    {
        var supplier = new Supplier(
            name: "Original Name",
            contactName: "Original Contact",
            identificationNumber: "ORIG-ID");
        _repository.Suppliers.Add(supplier);

        var result = await UseCases.UpdateDetailsAsync(
            id: supplier.Id,
            name: "   ",
            contactName: "New Contact");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Original Name", supplier.Name);
        Assert.Equal("Original Contact", supplier.ContactName);
        Assert.Equal("ORIG-ID", supplier.IdentificationNumber);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithSameIdentificationNumberOnSameSupplier_Succeeds()
    {
        var supplier = new Supplier("Acme Corp", identificationNumber: "TAX-SAME");
        _repository.Suppliers.Add(supplier);

        var result = await UseCases.UpdateDetailsAsync(
            id: supplier.Id,
            name: "Acme Corp Renamed",
            identificationNumber: "tax-same");

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Corp Renamed", supplier.Name);
        Assert.Equal("TAX-SAME", supplier.IdentificationNumber);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithDuplicateIdentificationNumberFromAnotherSupplier_ReturnsConflict()
    {
        var supplier1 = new Supplier("Supplier 1", identificationNumber: "TAX-SHARED");
        var supplier2 = new Supplier("Supplier 2", identificationNumber: "TAX-OTHER");
        _repository.Suppliers.AddRange([supplier1, supplier2]);

        var result = await UseCases.UpdateDetailsAsync(
            id: supplier2.Id,
            name: "Supplier 2 Renamed",
            identificationNumber: "tax-shared");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.SupplierIdentificationConflict, result.Error!.Code);
        Assert.Equal("A supplier with this identification number already exists.", result.Error!.Message);
        Assert.Equal("TAX-OTHER", supplier2.IdentificationNumber);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WhenSaveAsyncThrowsDuplicateSupplierIdentificationException_ReturnsConflict()
    {
        var supplier = new Supplier("Supplier", identificationNumber: "TAX-1");
        _repository.Suppliers.Add(supplier);
        _repository.SimulateSaveDuplicateException = true;

        var result = await UseCases.UpdateDetailsAsync(
            id: supplier.Id,
            name: "Supplier Updated",
            identificationNumber: "TAX-2");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.SupplierIdentificationConflict, result.Error!.Code);
        Assert.Equal("Simulated duplicate identification conflict.", result.Error!.Message);
    }

    [Fact]
    public async Task ActivateAsync_WithEmptyGuid_ReturnsInvalidInput()
    {
        var result = await UseCases.ActivateAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Supplier ID cannot be empty.", result.Error!.Message);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task ActivateAsync_WhenSupplierNotFound_ReturnsNotFound()
    {
        var result = await UseCases.ActivateAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Supplier was not found.", result.Error!.Message);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task ActivateAsync_WhenInactive_ActivatesSupplierAndSaves()
    {
        var supplier = new Supplier("Inactive Supplier");
        supplier.Deactivate();
        _repository.Suppliers.Add(supplier);

        var result = await UseCases.ActivateAsync(supplier.Id);

        Assert.True(result.IsSuccess);
        Assert.True(supplier.IsActive);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task DeactivateAsync_WithEmptyGuid_ReturnsInvalidInput()
    {
        var result = await UseCases.DeactivateAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal("Supplier ID cannot be empty.", result.Error!.Message);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task DeactivateAsync_WhenSupplierNotFound_ReturnsNotFound()
    {
        var result = await UseCases.DeactivateAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("Supplier was not found.", result.Error!.Message);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActive_DeactivatesSupplierAndSaves()
    {
        var supplier = new Supplier("Active Supplier");
        _repository.Suppliers.Add(supplier);

        var result = await UseCases.DeactivateAsync(supplier.Id);

        Assert.True(result.IsSuccess);
        Assert.False(supplier.IsActive);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task AllMethods_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        var supplier = new Supplier("Supplier");
        _repository.Suppliers.Add(supplier);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UseCases.CreateAsync("Supplier", cancellationToken: token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UseCases.GetByIdAsync(supplier.Id, cancellationToken: token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UseCases.ListAsync(cancellationToken: token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UseCases.UpdateDetailsAsync(supplier.Id, "New Name", cancellationToken: token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UseCases.ActivateAsync(supplier.Id, cancellationToken: token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UseCases.DeactivateAsync(supplier.Id, cancellationToken: token));
    }

    private sealed class InMemorySupplierRepository : ISupplierRepository
    {
        public List<Supplier> Suppliers { get; } = [];
        public int AddCount { get; private set; }
        public int SaveCount { get; private set; }
        public bool SimulateAddDuplicateException { get; set; }
        public bool SimulateSaveDuplicateException { get; set; }

        public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Suppliers.SingleOrDefault(s => s.Id == id));
        }

        public Task<IReadOnlyList<Supplier>> ListAsync(SupplierQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<Supplier> result = Suppliers;

            if (query.IsActive is bool isActive)
            {
                result = result.Where(s => s.IsActive == isActive);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                result = result.Where(s =>
                    s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (s.ContactName != null && s.ContactName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (s.IdentificationNumber != null && s.IdentificationNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            return Task.FromResult<IReadOnlyList<Supplier>>(result.ToList());
        }

        public Task<bool> ExistsByIdentificationNumberAsync(
            string normalizedIdentificationNumber,
            Guid? excludingId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var exists = Suppliers.Any(s =>
                s.Id != excludingId &&
                s.IdentificationNumber != null &&
                string.Equals(s.IdentificationNumber, normalizedIdentificationNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SimulateAddDuplicateException)
            {
                throw new DuplicateSupplierIdentificationException("Simulated duplicate identification conflict.");
            }

            Suppliers.Add(supplier);
            AddCount++;
            return Task.CompletedTask;
        }

        public Task SaveAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SimulateSaveDuplicateException)
            {
                throw new DuplicateSupplierIdentificationException("Simulated duplicate identification conflict.");
            }

            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
