using SistemaInventarioVentas.Application.Common;
using SistemaInventarioVentas.Application.Exceptions;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Application.UseCases;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Tests;

public sealed class CustomerUseCasesTests
{
    private readonly InMemoryCustomerRepository _repository = new();
    private CustomerUseCases UseCases => new(_repository);

    [Fact]
    public async Task CreateAsync_WithValidNameOnly_CreatesActiveCustomerAndCallsAdd()
    {
        var result = await UseCases.CreateAsync("  Acme Corporation  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Corporation", result.Value.Name);
        Assert.Null(result.Value.IdentificationNumber);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Phone);
        Assert.Null(result.Value.Address);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, _repository.AddCount);
        Assert.Contains(result.Value, _repository.Customers);
    }

    [Fact]
    public async Task CreateAsync_WithAllValidFields_NormalizesAndPersistsCustomer()
    {
        var createdAt = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

        var result = await UseCases.CreateAsync(
            name: "  Acme Corp  ",
            identificationNumber: "  tax-12345  ",
            email: "  CONTACT@ACME.COM  ",
            phone: "  +1-555-0199  ",
            address: "  123 Main St, Springfield  ",
            createdAtUtc: createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Corp", result.Value.Name);
        Assert.Equal("TAX-12345", result.Value.IdentificationNumber);
        Assert.NotNull(result.Value.Email);
        Assert.Equal("contact@acme.com", result.Value.Email.Value);
        Assert.Equal("+1-555-0199", result.Value.Phone);
        Assert.Equal("123 Main St, Springfield", result.Value.Address);
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
        var longName = new string('A', 201);

        var result = await UseCases.CreateAsync(longName);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidEmailFormat()
    {
        var result = await UseCases.CreateAsync("Acme Corp", email: "not-an-email");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsPhoneExceeding30Characters()
    {
        var longPhone = new string('9', 31);

        var result = await UseCases.CreateAsync("Acme Corp", phone: longPhone);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsAddressExceeding300Characters()
    {
        var longAddress = new string('X', 301);

        var result = await UseCases.CreateAsync("Acme Corp", address: longAddress);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsIdentificationNumberExceeding50Characters()
    {
        var longId = new string('1', 51);

        var result = await UseCases.CreateAsync("Acme Corp", identificationNumber: longId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateIdentificationNumber()
    {
        await UseCases.CreateAsync("Acme Corp", identificationNumber: "TAX-123");

        var duplicateResult = await UseCases.CreateAsync("Beta LLC", identificationNumber: " tax-123 ");

        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CustomerIdentificationConflict, duplicateResult.Error!.Code);
        Assert.Equal(1, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_TranslatesDuplicateCustomerIdentificationException_ToCustomerIdentificationConflict()
    {
        _repository.SimulateAddDuplicateException = true;

        var result = await UseCases.CreateAsync("Acme Corp", identificationNumber: "TAX-999");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CustomerIdentificationConflict, result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankOrWhitespaceIdentificationNumber_DoesNotCheckDuplicates(string? idNumber)
    {
        var result = await UseCases.CreateAsync("Acme Corp", identificationNumber: idNumber);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.IdentificationNumber);
        Assert.Equal(1, _repository.AddCount);
    }

    [Fact]
    public async Task CreateAsync_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            UseCases.CreateAsync("Acme Corp", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ReturnsSuccess()
    {
        var created = await UseCases.CreateAsync("Acme Corp");

        var result = await UseCases.GetByIdAsync(created.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Same(created.Value, result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        var result = await UseCases.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsEmpty_ReturnsInvalidInput()
    {
        var result = await UseCases.GetByIdAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task ListAsync_WithDefaultQuery_ReturnsAllCustomers()
    {
        var customer1 = (await UseCases.CreateAsync("Acme Corp")).Value;
        var customer2 = (await UseCases.CreateAsync("Beta LLC")).Value;

        var result = await UseCases.ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(customer1, result.Value);
        Assert.Contains(customer2, result.Value);
    }

    [Fact]
    public async Task ListAsync_WithIsActiveFilter_FiltersActiveOrInactive()
    {
        var active = (await UseCases.CreateAsync("Acme Corp")).Value;
        var inactive = (await UseCases.CreateAsync("Beta LLC")).Value;
        await UseCases.DeactivateAsync(inactive.Id);

        var activeResult = await UseCases.ListAsync(new CustomerQuery(IsActive: true));
        var inactiveResult = await UseCases.ListAsync(new CustomerQuery(IsActive: false));

        Assert.True(activeResult.IsSuccess);
        var activeCustomer = Assert.Single(activeResult.Value);
        Assert.Equal("Acme Corp", activeCustomer.Name);

        Assert.True(inactiveResult.IsSuccess);
        var inactiveCustomer = Assert.Single(inactiveResult.Value);
        Assert.Equal("Beta LLC", inactiveCustomer.Name);
    }

    [Fact]
    public async Task ListAsync_WithSearchTerm_FiltersByNameOrIdentification()
    {
        await UseCases.CreateAsync("Acme Corp", identificationNumber: "ID-100");
        await UseCases.CreateAsync("Beta Industries", identificationNumber: "ID-200");
        await UseCases.CreateAsync("Gamma Corp", identificationNumber: "ID-300");

        var byName = await UseCases.ListAsync(new CustomerQuery(SearchTerm: "  corp  "));
        var byId = await UseCases.ListAsync(new CustomerQuery(SearchTerm: "  200  "));

        Assert.True(byName.IsSuccess);
        Assert.Equal(2, byName.Value.Count);
        Assert.All(byName.Value, c => Assert.Contains("Corp", c.Name));

        Assert.True(byId.IsSuccess);
        var idCustomer = Assert.Single(byId.Value);
        Assert.Equal("Beta Industries", idCustomer.Name);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithValidData_UpdatesCustomerAndCallsSave()
    {
        var created = (await UseCases.CreateAsync("Acme Corp", identificationNumber: "OLD-ID")).Value;

        var result = await UseCases.UpdateDetailsAsync(
            id: created.Id,
            name: "Acme International",
            identificationNumber: "new-id",
            email: "info@acme.com",
            phone: "+1-555-9999",
            address: "456 Elm St");

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme International", result.Value.Name);
        Assert.Equal("NEW-ID", result.Value.IdentificationNumber);
        Assert.NotNull(result.Value.Email);
        Assert.Equal("info@acme.com", result.Value.Email.Value);
        Assert.Equal("+1-555-9999", result.Value.Phone);
        Assert.Equal("456 Elm St", result.Value.Address);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        var result = await UseCases.UpdateDetailsAsync(
            Guid.NewGuid(),
            "Non Existent");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WhenIdIsEmpty_ReturnsInvalidInput()
    {
        var result = await UseCases.UpdateDetailsAsync(
            Guid.Empty,
            "Acme Corp");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_RejectsDuplicateIdentificationNumberFromAnotherCustomer()
    {
        var customer1 = (await UseCases.CreateAsync("Acme Corp", identificationNumber: "ID-1")).Value;
        var customer2 = (await UseCases.CreateAsync("Beta LLC", identificationNumber: "ID-2")).Value;

        var result = await UseCases.UpdateDetailsAsync(
            customer2.Id,
            "Beta LLC",
            identificationNumber: "id-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CustomerIdentificationConflict, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_TranslatesDuplicateCustomerIdentificationException_ToCustomerIdentificationConflict()
    {
        var customer = (await UseCases.CreateAsync("Acme Corp", identificationNumber: "ID-1")).Value;
        _repository.SimulateSaveDuplicateException = true;

        var result = await UseCases.UpdateDetailsAsync(
            customer.Id,
            "Acme Corp",
            identificationNumber: "ID-999");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.CustomerIdentificationConflict, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateDetailsAsync_AllowsSameIdentificationNumberForCurrentCustomer()
    {
        var customer = (await UseCases.CreateAsync("Acme Corp", identificationNumber: "ID-1")).Value;

        var result = await UseCases.UpdateDetailsAsync(
            customer.Id,
            "Acme Renamed",
            identificationNumber: "id-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Renamed", result.Value.Name);
        Assert.Equal("ID-1", result.Value.IdentificationNumber);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_AllowsClearingOptionalFields()
    {
        var customer = (await UseCases.CreateAsync(
            name: "Acme Corp",
            identificationNumber: "ID-1",
            email: "test@acme.com",
            phone: "123456",
            address: "Main St")).Value;

        var result = await UseCases.UpdateDetailsAsync(
            id: customer.Id,
            name: "Acme Corp",
            identificationNumber: null,
            email: null,
            phone: null,
            address: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.IdentificationNumber);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Phone);
        Assert.Null(result.Value.Address);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDetailsAsync_RejectsBlankName(string? name)
    {
        var customer = (await UseCases.CreateAsync("Acme Corp")).Value;

        var result = await UseCases.UpdateDetailsAsync(customer.Id, name!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateDetailsAsync_RejectsInvalidEmail()
    {
        var customer = (await UseCases.CreateAsync("Acme Corp")).Value;

        var result = await UseCases.UpdateDetailsAsync(customer.Id, "Acme Corp", email: "invalid-email");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task ActivateAsync_WhenCustomerExists_ActivatesAndCallsSave()
    {
        var customer = (await UseCases.CreateAsync("Acme Corp")).Value;
        await UseCases.DeactivateAsync(customer.Id);
        Assert.False(customer.IsActive);

        var result = await UseCases.ActivateAsync(customer.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(2, _repository.SaveCount);
    }

    [Fact]
    public async Task ActivateAsync_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        var result = await UseCases.ActivateAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ActivateAsync_WhenIdIsEmpty_ReturnsInvalidInput()
    {
        var result = await UseCases.ActivateAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCustomerExists_DeactivatesAndCallsSave()
    {
        var customer = (await UseCases.CreateAsync("Acme Corp")).Value;
        Assert.True(customer.IsActive);

        var result = await UseCases.DeactivateAsync(customer.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        var result = await UseCases.DeactivateAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.NotFound, result.Error!.Code);
    }

    [Fact]
    public async Task DeactivateAsync_WhenIdIsEmpty_ReturnsInvalidInput()
    {
        var result = await UseCases.DeactivateAsync(Guid.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InvalidInput, result.Error!.Code);
    }

    private sealed class InMemoryCustomerRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = [];
        public int AddCount { get; private set; }
        public int SaveCount { get; private set; }
        public bool SimulateAddDuplicateException { get; set; }
        public bool SimulateSaveDuplicateException { get; set; }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Customers.SingleOrDefault(c => c.Id == id));
        }

        public Task<IReadOnlyList<Customer>> ListAsync(CustomerQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<Customer> result = Customers;

            if (query.IsActive is bool isActive)
            {
                result = result.Where(c => c.IsActive == isActive);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                result = result.Where(c =>
                    c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.IdentificationNumber != null && c.IdentificationNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            return Task.FromResult<IReadOnlyList<Customer>>(result.ToList());
        }

        public Task<bool> ExistsByIdentificationNumberAsync(
            string normalizedIdentificationNumber,
            Guid? excludingId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var exists = Customers.Any(c =>
                c.Id != excludingId &&
                c.IdentificationNumber != null &&
                string.Equals(c.IdentificationNumber, normalizedIdentificationNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SimulateAddDuplicateException)
            {
                throw new DuplicateCustomerIdentificationException("Simulated duplicate identification conflict.");
            }

            Customers.Add(customer);
            AddCount++;
            return Task.CompletedTask;
        }

        public Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SimulateSaveDuplicateException)
            {
                throw new DuplicateCustomerIdentificationException("Simulated duplicate identification conflict.");
            }

            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
