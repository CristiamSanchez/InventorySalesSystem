using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaInventarioVentas.API.Contracts;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.API.Tests;

public sealed class SupplierApiTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateSupplier_WithAllFields_ReturnsCreatedAndPersists()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateSupplierRequest
        {
            Name = "Global Logistics Suppliers",
            ContactName = "Jane Doe",
            IdentificationNumber = "RFC-20512345678",
            Email = "contact@globalsuppliers.com",
            Phone = "+51 987654321",
            Address = "Av. Industrial 456, Lima"
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/suppliers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.Equal("Global Logistics Suppliers", supplier.Name);
        Assert.Equal("Jane Doe", supplier.ContactName);
        Assert.Equal("RFC-20512345678", supplier.IdentificationNumber);
        Assert.Equal("contact@globalsuppliers.com", supplier.Email);
        Assert.Equal("+51 987654321", supplier.Phone);
        Assert.Equal("Av. Industrial 456, Lima", supplier.Address);
        Assert.True(supplier.IsActive);
        Assert.Contains(supplier.Id.ToString(), response.Headers.Location.ToString());

        // Verify persistence directly in database
        await using var dbContext = fixture.CreateDbContext();
        var entity = await dbContext.Suppliers.SingleOrDefaultAsync(s => s.Id == supplier.Id);
        Assert.NotNull(entity);
        Assert.Equal("Global Logistics Suppliers", entity.Name);
        Assert.Equal("Jane Doe", entity.ContactName);
        Assert.Equal("RFC-20512345678", entity.IdentificationNumber);
        Assert.Equal("contact@globalsuppliers.com", entity.Email?.Value);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public async Task CreateSupplier_WithMinimalFields_ReturnsCreated()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateSupplierRequest
        {
            Name = "Minimal Supplier"
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/suppliers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        Assert.Equal("Minimal Supplier", supplier.Name);
        Assert.Null(supplier.ContactName);
        Assert.Null(supplier.IdentificationNumber);
        Assert.Null(supplier.Email);
        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Address);
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public async Task CreateSupplier_WhenDuplicateIdentification_ReturnsConflict409()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var first = new CreateSupplierRequest
        {
            Name = "First Supplier",
            IdentificationNumber = "TAX-SUP-998877"
        };
        using var firstResponse = await fixture.Client.PostAsJsonAsync("/api/suppliers", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Attempt duplicate with lowercase variation
        var second = new CreateSupplierRequest
        {
            Name = "Second Supplier",
            IdentificationNumber = "tax-sup-998877"
        };
        using var secondResponse = await fixture.Client.PostAsJsonAsync("/api/suppliers", second);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Supplier conflict", problem.Title);
        Assert.Contains("already exists", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSupplier_AllowsMultipleSuppliersWithoutIdentificationNumber()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var first = new CreateSupplierRequest { Name = "Supplier Alpha", IdentificationNumber = null };
        var second = new CreateSupplierRequest { Name = "Supplier Beta", IdentificationNumber = null };

        using var firstResponse = await fixture.Client.PostAsJsonAsync("/api/suppliers", first);
        using var secondResponse = await fixture.Client.PostAsJsonAsync("/api/suppliers", second);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSupplier_WithInvalidEmail_ReturnsValidationProblem400()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateSupplierRequest
        {
            Name = "Valid Supplier Name",
            Email = "invalid-email-string"
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/suppliers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateSupplierRequest.Email)) || problem.Errors.ContainsKey("Email"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateSupplier_WithBlankName_ReturnsValidationProblem400(string? name)
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateSupplierRequest
        {
            Name = name
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/suppliers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateSupplierRequest.Name)) || problem.Errors.ContainsKey("Name"));
    }

    [Fact]
    public async Task CreateSupplier_WithOversizedFields_ReturnsValidationProblem400()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var request = new CreateSupplierRequest
        {
            Name = new string('A', 201),
            ContactName = new string('C', 201),
            IdentificationNumber = new string('I', 51),
            Phone = new string('1', 31),
            Address = new string('X', 301)
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/suppliers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.Count >= 4);
    }

    [Fact]
    public async Task GetSupplierById_WhenExists_ReturnsOk200()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var created = await CreateTestSupplierAsync("Lookup Supplier", "TAX-LOOKUP");

        using var response = await fixture.Client.GetAsync($"/api/suppliers/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        Assert.Equal(created.Id, supplier.Id);
        Assert.Equal("Lookup Supplier", supplier.Name);
        Assert.Equal("TAX-LOOKUP", supplier.IdentificationNumber);
    }

    [Fact]
    public async Task GetSupplierById_WhenNotFound_ReturnsNotFound404()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        using var response = await fixture.Client.GetAsync($"/api/suppliers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found", problem.Title);
    }

    [Fact]
    public async Task ListSuppliers_ReturnsAllSuppliers()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var s1 = await CreateTestSupplierAsync("Supplier One");
        var s2 = await CreateTestSupplierAsync("Supplier Two");

        using var response = await fixture.Client.GetAsync("/api/suppliers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);
        Assert.Contains(list, s => s.Id == s1.Id);
        Assert.Contains(list, s => s.Id == s2.Id);
    }

    [Fact]
    public async Task ListSuppliers_WithIsActiveFilter_FiltersCorrectly()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var active = await CreateTestSupplierAsync("Active Supplier");
        var inactive = await CreateTestSupplierAsync("To Inactive");
        using var deactResponse = await fixture.Client.PostAsync($"/api/suppliers/{inactive.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactResponse.StatusCode);

        using var activeResponse = await fixture.Client.GetAsync("/api/suppliers?isActive=true");
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);
        var activeList = await activeResponse.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(activeList);
        Assert.Contains(activeList, s => s.Id == active.Id);
        Assert.DoesNotContain(activeList, s => s.Id == inactive.Id);

        using var inactiveResponse = await fixture.Client.GetAsync("/api/suppliers?isActive=false");
        Assert.Equal(HttpStatusCode.OK, inactiveResponse.StatusCode);
        var inactiveList = await inactiveResponse.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(inactiveList);
        Assert.Contains(inactiveList, s => s.Id == inactive.Id);
        Assert.DoesNotContain(inactiveList, s => s.Id == active.Id);
    }

    [Fact]
    public async Task ListSuppliers_WithSearchTerm_MatchesCaseInsensitively()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var s1 = await CreateTestSupplierAsync(
            name: "Industrial Alpha",
            identificationNumber: "TAX-ALPHA-1",
            contactName: "John Connor",
            email: "john@industrial.com",
            phone: "+1-555-1010");

        var s2 = await CreateTestSupplierAsync(
            name: "Beta Logistics",
            identificationNumber: "RFC-BETA-2",
            contactName: "Sarah Connor",
            email: "sarah@betalog.com",
            phone: "+1-555-2020");

        // Search by Name
        using var nameSearch = await fixture.Client.GetAsync("/api/suppliers?searchTerm=alpha");
        var nameList = await nameSearch.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(nameList);
        Assert.Single(nameList);
        Assert.Equal(s1.Id, nameList[0].Id);

        // Search by ContactName
        using var contactSearch = await fixture.Client.GetAsync("/api/suppliers?searchTerm=sarah");
        var contactList = await contactSearch.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(contactList);
        Assert.Single(contactList);
        Assert.Equal(s2.Id, contactList[0].Id);

        // Search by IdentificationNumber
        using var idSearch = await fixture.Client.GetAsync("/api/suppliers?searchTerm=alpha-1");
        var idList = await idSearch.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(idList);
        Assert.Single(idList);
        Assert.Equal(s1.Id, idList[0].Id);

        // Search by Email
        using var emailSearch = await fixture.Client.GetAsync("/api/suppliers?searchTerm=betalog.com");
        var emailList = await emailSearch.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(emailList);
        Assert.Single(emailList);
        Assert.Equal(s2.Id, emailList[0].Id);

        // Search by Phone
        using var phoneSearch = await fixture.Client.GetAsync("/api/suppliers?searchTerm=1010");
        var phoneList = await phoneSearch.Content.ReadFromJsonAsync<List<SupplierResponse>>(JsonOptions);
        Assert.NotNull(phoneList);
        Assert.Single(phoneList);
        Assert.Equal(s1.Id, phoneList[0].Id);
    }

    [Fact]
    public async Task UpdateSupplier_WithValidFields_UpdatesAndReturnsOk200()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var created = await CreateTestSupplierAsync("Original Supplier", "TAX-ORIG");

        var updateRequest = new UpdateSupplierRequest
        {
            Name = "Updated Supplier Name",
            ContactName = "New Contact Person",
            IdentificationNumber = "TAX-UPDATED",
            Email = "new@supplier.com",
            Phone = "+1-555-9999",
            Address = "New Address 123"
        };

        using var response = await fixture.Client.PutAsJsonAsync($"/api/suppliers/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Supplier Name", updated.Name);
        Assert.Equal("New Contact Person", updated.ContactName);
        Assert.Equal("TAX-UPDATED", updated.IdentificationNumber);
        Assert.Equal("new@supplier.com", updated.Email);
        Assert.Equal("+1-555-9999", updated.Phone);
        Assert.Equal("New Address 123", updated.Address);

        // Verify in database
        await using var dbContext = fixture.CreateDbContext();
        var entity = await dbContext.Suppliers.SingleOrDefaultAsync(s => s.Id == created.Id);
        Assert.NotNull(entity);
        Assert.Equal("Updated Supplier Name", entity.Name);
        Assert.Equal("New Contact Person", entity.ContactName);
        Assert.Equal("TAX-UPDATED", entity.IdentificationNumber);
    }

    [Fact]
    public async Task UpdateSupplier_WhenDuplicateIdentificationOnAnotherSupplier_ReturnsConflict409()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var s1 = await CreateTestSupplierAsync("Supplier One", "TAX-SHARED-1");
        var s2 = await CreateTestSupplierAsync("Supplier Two", "TAX-SHARED-2");

        var updateRequest = new UpdateSupplierRequest
        {
            Name = "Supplier Two Renamed",
            IdentificationNumber = "tax-shared-1"
        };

        using var response = await fixture.Client.PutAsJsonAsync($"/api/suppliers/{s2.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Supplier conflict", problem.Title);
    }

    [Fact]
    public async Task UpdateSupplier_WhenSameIdentificationOnSelf_ReturnsOk200()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var created = await CreateTestSupplierAsync("Supplier Self", "TAX-SELF-99");

        var updateRequest = new UpdateSupplierRequest
        {
            Name = "Supplier Self Renamed",
            IdentificationNumber = "tax-self-99"
        };

        using var response = await fixture.Client.PutAsJsonAsync($"/api/suppliers/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Supplier Self Renamed", updated.Name);
        Assert.Equal("TAX-SELF-99", updated.IdentificationNumber);
    }

    [Fact]
    public async Task UpdateSupplier_WhenNotFound_ReturnsNotFound404()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var updateRequest = new UpdateSupplierRequest
        {
            Name = "Missing Supplier"
        };

        using var response = await fixture.Client.PutAsJsonAsync($"/api/suppliers/{Guid.NewGuid()}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ActivateSupplier_WhenInactive_ActivatesAndReturnsOk200()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var created = await CreateTestSupplierAsync("Supplier To Deact");
        using var deact = await fixture.Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deact.StatusCode);

        using var act = await fixture.Client.PostAsync($"/api/suppliers/{created.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, act.StatusCode);
        var activated = await act.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(activated);
        Assert.True(activated.IsActive);
    }

    [Fact]
    public async Task DeactivateSupplier_WhenActive_DeactivatesAndReturnsOk200()
    {
        await fixture.ResetAsAsync(UserRole.Admin);

        var created = await CreateTestSupplierAsync("Active Supplier");

        using var deact = await fixture.Client.PostAsync($"/api/suppliers/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deact.StatusCode);
        var deactivated = await deact.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(deactivated);
        Assert.False(deactivated.IsActive);
    }

    [Fact]
    public async Task AnonymousAccess_ReturnsUnauthorized401()
    {
        await fixture.ResetAsync();

        using var getResponse = await fixture.Client.GetAsync("/api/suppliers");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        using var postResponse = await fixture.Client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { Name = "Test" });
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
    }

    [Fact]
    public async Task UserRole_CanRead_ButCannotMutate()
    {
        // First create as Admin
        await fixture.ResetAsAsync(UserRole.Admin);
        var supplier = await CreateTestSupplierAsync("Target Supplier");

        // Switch to User role (non-admin)
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("User"));

        // Reads allowed
        using var getList = await fixture.Client.GetAsync("/api/suppliers");
        Assert.Equal(HttpStatusCode.OK, getList.StatusCode);

        using var getById = await fixture.Client.GetAsync($"/api/suppliers/{supplier.Id}");
        Assert.Equal(HttpStatusCode.OK, getById.StatusCode);

        // Mutations forbidden (403)
        using var create = await fixture.Client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest { Name = "Forbidden" });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        using var update = await fixture.Client.PutAsJsonAsync($"/api/suppliers/{supplier.Id}", new UpdateSupplierRequest { Name = "Forbidden" });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        using var activate = await fixture.Client.PostAsync($"/api/suppliers/{supplier.Id}/activate", null);
        Assert.Equal(HttpStatusCode.Forbidden, activate.StatusCode);

        using var deactivate = await fixture.Client.PostAsync($"/api/suppliers/{supplier.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
    }

    private string CreateToken(string role)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "SistemaInventarioVentas.Api.Tests",
            audience: "SistemaInventarioVentas.Api.Tests.Client",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString("D")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
                new Claim("role", role)
            ],
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fixture.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<SupplierResponse> CreateTestSupplierAsync(
        string name,
        string? identificationNumber = null,
        string? contactName = null,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        var request = new CreateSupplierRequest
        {
            Name = name,
            ContactName = contactName,
            IdentificationNumber = identificationNumber,
            Email = email,
            Phone = phone,
            Address = address
        };

        using var response = await fixture.Client.PostAsJsonAsync("/api/suppliers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        return supplier;
    }
}
